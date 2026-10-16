using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using JardinNoemi.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JardinNoemi.Services;

public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(Guid cartId, CheckoutForm form, string? userId, CancellationToken ct = default);

    /// <summary>
    /// Devuelve el pedido solo si quien lo solicita está autorizado: el dueño autenticado, un administrador
    /// o quien presenta el token de acceso generado al comprar (compras como invitado).
    /// </summary>
    Task<OrderDetailDto?> GetOrderForViewerAsync(string orderNumber, string? accessToken, string? userId, bool isAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForUserAsync(string userId, CancellationToken ct = default);
}

public sealed class OrderService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IShippingCalculator shipping,
    IPaymentGateway paymentGateway,
    IOrderNotifier notifier,
    IOptions<ShopOptions> store,
    ILogger<OrderService> logger) : IOrderService
{
    private const string OrderCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<PlaceOrderResult> PlaceOrderAsync(Guid cartId, CheckoutForm form, string? userId, CancellationToken ct = default)
    {
        // 1. Validación del lado del servidor (nunca se confía solo en la del navegador).
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(form, new ValidationContext(form), validation, validateAllProperties: true))
        {
            return PlaceOrderResult.Fail("Revisa los datos del formulario: " + validation.First().ErrorMessage);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // 2. Líneas del carrito con precios y stock actuales (los precios del cliente se ignoran).
        var lines = await db.CartItems
            .Where(i => i.CartId == cartId)
            .Select(i => new
            {
                i.ProductId,
                i.Quantity,
                i.Product!.Name,
                i.Product.Slug,
                i.Product.Sku,
                i.Product.Price,
                i.Product.Stock,
                Available = i.Product.IsActive && i.Product.Category!.IsActive,
                ImageUrl = i.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault()
            })
            .ToListAsync(ct);

        if (lines.Count == 0)
        {
            return PlaceOrderResult.Fail("Tu carrito está vacío.", cartChanged: true);
        }

        var problem = lines.FirstOrDefault(l => !l.Available || l.Stock < l.Quantity);
        if (problem is not null)
        {
            var reason = !problem.Available || problem.Stock == 0
                ? $"{problem.Name} ya no está disponible."
                : $"Solo quedan {problem.Stock} unidades de {problem.Name}.";
            return PlaceOrderResult.Fail(reason + " Actualizamos tu carrito para que puedas continuar.", cartChanged: true);
        }

        var subtotal = lines.Sum(l => l.Price * l.Quantity);
        var quote = shipping.Quote(subtotal, form.DeliveryMethod, form.Region);
        var now = DateTime.UtcNow;

        Order order;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var customer = await UpsertCustomerAsync(db, form, userId, ct);

            // 3. Descuento de stock atómico: solo si aún hay unidades suficientes.
            foreach (var line in lines)
            {
                var updated = await db.Products
                    .Where(p => p.Id == line.ProductId && p.Stock >= line.Quantity)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Stock, p => p.Stock - line.Quantity)
                        .SetProperty(p => p.UpdatedAt, now), ct);

                if (updated == 0)
                {
                    await tx.RollbackAsync(ct);
                    return PlaceOrderResult.Fail($"Alguien se nos adelantó: {line.Name} acaba de agotarse. Revisa tu carrito.", cartChanged: true);
                }
            }

            order = new Order
            {
                OrderNumber = await GenerateOrderNumberAsync(db, ct),
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
                Customer = customer,
                UserId = userId,
                CustomerName = $"{form.FirstName.Trim()} {form.LastName.Trim()}",
                Email = form.Email.Trim(),
                Phone = form.Phone.Trim(),
                DeliveryMethod = form.DeliveryMethod,
                ShippingAddress = form.DeliveryMethod == DeliveryMethod.HomeDelivery
                    ? new ShippingAddress
                    {
                        Street = form.Street!.Trim(),
                        Apartment = form.Apartment?.Trim(),
                        City = form.City!.Trim(),
                        Region = form.Region!,
                        PostalCode = form.PostalCode?.Trim()
                    }
                    : null,
                Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
                Subtotal = subtotal,
                ShippingCost = quote.Cost,
                Total = subtotal + quote.Cost,
                PaymentMethod = form.PaymentMethod,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
                Items = lines.Select(l => new OrderItem
                {
                    ProductId = l.ProductId,
                    ProductName = l.Name,
                    ProductSlug = l.Slug,
                    Sku = l.Sku,
                    ImageUrl = l.ImageUrl,
                    UnitPrice = l.Price,
                    Quantity = l.Quantity,
                    LineTotal = l.Price * l.Quantity
                }).ToList()
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        // 4. Pago (fuera de la transacción: nunca se mantiene una transacción abierta durante una llamada externa).
        if (form.PaymentMethod == PaymentMethod.BankTransfer)
        {
            order.PaymentStatus = PaymentStatus.AwaitingTransfer;
            order.PaymentProvider = "Transferencia bancaria";
        }
        else
        {
            var token = form.PaymentToken
                        ?? (paymentGateway as ITestCardTokenizer)?.Tokenize(form.CardNumber);
            PaymentResult payment;
            if (string.IsNullOrEmpty(token))
            {
                payment = new PaymentResult(PaymentOutcome.Error, null, "No se recibió un medio de pago válido.");
            }
            else
            {
                try
                {
                    payment = await paymentGateway.ChargeAsync(new PaymentRequest(
                        order.OrderNumber, order.Total, store.Value.CurrencyCode, order.Email, token,
                        $"{store.Value.Name} - pedido {order.OrderNumber}"), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error de pasarela para el pedido {OrderNumber}", order.OrderNumber);
                    payment = new PaymentResult(PaymentOutcome.Error, null, "No pudimos procesar el pago. Inténtalo nuevamente.");
                }
            }

            order.PaymentProvider = paymentGateway.ProviderName;
            if (!payment.IsApproved)
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(CancellationToken.None);
                await RestockAsync(db, order, CancellationToken.None);
                logger.LogInformation("Pago no aprobado para {OrderNumber}: {Outcome}", order.OrderNumber, payment.Outcome);
                return PlaceOrderResult.Fail(payment.Message ?? "El pago no fue aprobado.");
            }

            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentReference = payment.TransactionId;
            order.Status = OrderStatus.Confirmed;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        // 5. El pedido está confirmado: se vacía el carrito.
        await db.CartItems.Where(i => i.CartId == cartId).ExecuteDeleteAsync(CancellationToken.None);

        await notifier.OrderPlacedAsync(order.OrderNumber, order.Email);
        logger.LogInformation("Pedido {OrderNumber} creado por {Total}", order.OrderNumber, order.Total);
        return new PlaceOrderResult(true, order.OrderNumber, order.AccessToken, null);
    }

    public async Task<OrderDetailDto?> GetOrderForViewerAsync(string orderNumber, string? accessToken, string? userId, bool isAdmin, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
        if (order is null) return null;

        var authorized = isAdmin
                         || (userId is not null && order.UserId == userId)
                         || (accessToken is not null && TokensMatch(accessToken, order.AccessToken));
        return authorized ? ToDetail(order) : null;
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id, o.OrderNumber, o.CreatedAt, o.Status, o.PaymentStatus, o.Total,
                o.Items.Sum(i => i.Quantity),
                o.Items.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()))
            .ToListAsync(ct);
    }

    internal static OrderDetailDto ToDetail(Order o) => new(
        o.Id, o.OrderNumber, o.CreatedAt, o.Status, o.PaymentStatus, o.PaymentMethod, o.PaymentProvider, o.PaymentReference,
        o.DeliveryMethod, o.CustomerName, o.Email, o.Phone, o.ShippingAddress, o.Notes,
        o.Subtotal, o.ShippingCost, o.Total,
        o.Items.OrderBy(i => i.Id)
            .Select(i => new OrderLineDto(i.ProductName, i.ProductSlug, i.ImageUrl, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList());

    private static async Task<Customer> UpsertCustomerAsync(ApplicationDbContext db, CheckoutForm form, string? userId, CancellationToken ct)
    {
        var email = form.Email.Trim();
        Customer? customer = userId is not null
            ? await db.Customers.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.UserId == userId, ct)
            // Invitados: se reutiliza el perfil de invitado con el mismo correo (nunca el de un usuario registrado).
            : await db.Customers.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.UserId == null && c.Email == email, ct);

        if (customer is null)
        {
            customer = new Customer
            {
                UserId = userId,
                Email = email,
                FirstName = form.FirstName.Trim(),
                LastName = form.LastName.Trim(),
                Phone = form.Phone.Trim()
            };
            db.Customers.Add(customer);
        }
        else
        {
            customer.FirstName = form.FirstName.Trim();
            customer.LastName = form.LastName.Trim();
            customer.Phone = form.Phone.Trim();
        }

        if (form.DeliveryMethod == DeliveryMethod.HomeDelivery && customer.Addresses.Count == 0)
        {
            customer.Addresses.Add(new Address
            {
                Street = form.Street!.Trim(),
                Apartment = form.Apartment?.Trim(),
                City = form.City!.Trim(),
                Region = form.Region!,
                PostalCode = form.PostalCode?.Trim(),
                IsDefault = true
            });
        }

        return customer;
    }

    private static async Task RestockAsync(ApplicationDbContext db, Order order, CancellationToken ct)
    {
        foreach (var item in order.Items.Where(i => i.ProductId is not null))
        {
            await db.Products.Where(p => p.Id == item.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
        }
    }

    private static async Task<string> GenerateOrderNumberAsync(ApplicationDbContext db, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = new StringBuilder(4);
            for (var i = 0; i < 4; i++) code.Append(OrderCodeAlphabet[RandomNumberGenerator.GetInt32(OrderCodeAlphabet.Length)]);
            var number = $"JN-{DateTime.UtcNow:yyMMdd}-{code}";
            if (!await db.Orders.AnyAsync(o => o.OrderNumber == number, ct)) return number;
        }
        throw new InvalidOperationException("No se pudo generar un número de pedido único.");
    }

    private static bool TokensMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
}

/// <summary>
/// Punto de extensión para notificaciones (correo, WhatsApp, webhook al taller).
/// La implementación por defecto solo registra en el log.
/// </summary>
public interface IOrderNotifier
{
    Task OrderPlacedAsync(string orderNumber, string customerEmail);
}

public sealed class LoggingOrderNotifier(ILogger<LoggingOrderNotifier> logger) : IOrderNotifier
{
    public Task OrderPlacedAsync(string orderNumber, string customerEmail)
    {
        logger.LogInformation("Notificación pendiente de integrar: confirmación del pedido {OrderNumber}", orderNumber);
        return Task.CompletedTask;
    }
}
