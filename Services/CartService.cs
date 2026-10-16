using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;

namespace JardinNoemi.Services;

public interface ICartService
{
    /// <summary>
    /// Determina el carrito vigente. Si hay usuario autenticado y un carrito anónimo previo,
    /// fusiona ambos en el carrito del usuario. Devuelve null si aún no existe carrito.
    /// </summary>
    Task<CartResolution> ResolveCartAsync(Guid? anonymousCartId, string? userId, CancellationToken ct = default);

    Task<Guid> CreateCartAsync(string? userId, CancellationToken ct = default);
    Task<CartDto> GetCartAsync(Guid? cartId, CancellationToken ct = default);
    Task<CartResult> AddItemAsync(Guid cartId, int productId, int quantity, CancellationToken ct = default);
    Task<CartResult> SetQuantityAsync(Guid cartId, int productId, int quantity, CancellationToken ct = default);
    Task<CartResult> RemoveItemAsync(Guid cartId, int productId, CancellationToken ct = default);
    Task ClearAsync(Guid cartId, CancellationToken ct = default);
}

public sealed record CartResolution(Guid? CartId, bool AnonymousCartConsumed);

public sealed class CartService(IDbContextFactory<ApplicationDbContext> dbFactory) : ICartService
{
    /// <summary>Máximo de unidades por producto en un pedido (evita acaparamiento y errores de tipeo).</summary>
    public const int MaxQuantityPerLine = 20;

    public async Task<CartResolution> ResolveCartAsync(Guid? anonymousCartId, string? userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Un id anónimo nunca puede dar acceso al carrito de un usuario registrado.
        Cart? anonymous = null;
        if (anonymousCartId is { } anonId)
        {
            anonymous = await db.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == anonId && c.UserId == null, ct);
        }

        if (userId is null)
        {
            return new CartResolution(anonymous?.Id, false);
        }

        var userCart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (anonymous is null)
        {
            return new CartResolution(userCart?.Id, anonymousCartId is not null);
        }

        if (userCart is null)
        {
            // El carrito anónimo pasa a pertenecer al usuario.
            anonymous.UserId = userId;
            anonymous.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new CartResolution(anonymous.Id, true);
        }

        // Fusión: se suman cantidades respetando stock y máximo por línea.
        var productIds = anonymous.Items.Select(i => i.ProductId).ToList();
        var stock = await db.Products.Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Stock }).ToDictionaryAsync(p => p.Id, p => p.Stock, ct);

        foreach (var item in anonymous.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            var limit = Math.Min(MaxQuantityPerLine, stock.GetValueOrDefault(item.ProductId));
            if (existing is null)
            {
                if (limit > 0)
                {
                    userCart.Items.Add(new CartItem { ProductId = item.ProductId, Quantity = Math.Min(item.Quantity, limit) });
                }
            }
            else
            {
                existing.Quantity = Math.Max(1, Math.Min(existing.Quantity + item.Quantity, Math.Max(limit, existing.Quantity)));
            }
        }

        userCart.UpdatedAt = DateTime.UtcNow;
        db.Carts.Remove(anonymous);
        await db.SaveChangesAsync(ct);
        return new CartResolution(userCart.Id, true);
    }

    public async Task<Guid> CreateCartAsync(string? userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (userId is not null)
        {
            var existing = await db.Carts.Where(c => c.UserId == userId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
            if (existing is { } id) return id;
        }

        var cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return cart.Id;
    }

    public async Task<CartDto> GetCartAsync(Guid? cartId, CancellationToken ct = default)
    {
        if (cartId is null) return CartDto.Empty;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await LoadAsync(db, cartId.Value, ct);
    }

    public async Task<CartResult> AddItemAsync(Guid cartId, int productId, int quantity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        quantity = Math.Clamp(quantity, 1, MaxQuantityPerLine);

        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId && p.IsActive && p.Category!.IsActive)
            .Select(p => new { p.Id, p.Name, p.Stock })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            return CartResult.Fail(await LoadAsync(db, cartId, ct), "Este producto ya no está disponible.");
        if (product.Stock <= 0)
            return CartResult.Fail(await LoadAsync(db, cartId, ct), $"{product.Name} está agotado por ahora.");

        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
            return CartResult.Fail(CartDto.Empty, "No encontramos tu carrito. Recarga la página e inténtalo de nuevo.");

        var limit = Math.Min(MaxQuantityPerLine, product.Stock);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var current = item?.Quantity ?? 0;
        if (current >= limit)
        {
            return CartResult.Fail(await LoadAsync(db, cartId, ct),
                $"Ya tienes el máximo disponible de {product.Name} en tu carrito ({limit}).");
        }

        var newQuantity = Math.Min(current + quantity, limit);
        if (item is null)
        {
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = newQuantity });
        }
        else
        {
            item.Quantity = newQuantity;
        }
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var message = newQuantity - current < quantity
            ? $"Agregamos {newQuantity - current} de {quantity}: es el stock disponible de {product.Name}."
            : null;
        return CartResult.Ok(await LoadAsync(db, cartId, ct), message);
    }

    public async Task<CartResult> SetQuantityAsync(Guid cartId, int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0) return await RemoveItemAsync(cartId, productId, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var item = await db.CartItems.Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId, ct);
        if (item is null) return CartResult.Fail(await LoadAsync(db, cartId, ct), "El producto ya no está en tu carrito.");

        var limit = Math.Min(MaxQuantityPerLine, item.Product?.Stock ?? 0);
        string? message = null;
        if (quantity > limit)
        {
            quantity = Math.Max(1, limit);
            message = limit > 0 ? $"Solo quedan {limit} unidades disponibles." : "Este producto se agotó.";
        }

        item.Quantity = quantity;
        await db.SaveChangesAsync(ct);
        var cart = await LoadAsync(db, cartId, ct);
        return message is null ? CartResult.Ok(cart) : CartResult.Fail(cart, message);
    }

    public async Task<CartResult> RemoveItemAsync(Guid cartId, int productId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.CartItems.Where(i => i.CartId == cartId && i.ProductId == productId).ExecuteDeleteAsync(ct);
        return CartResult.Ok(await LoadAsync(db, cartId, ct));
    }

    public async Task ClearAsync(Guid cartId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.CartItems.Where(i => i.CartId == cartId).ExecuteDeleteAsync(ct);
    }

    private static async Task<CartDto> LoadAsync(ApplicationDbContext db, Guid cartId, CancellationToken ct)
    {
        var lines = await db.CartItems.AsNoTracking()
            .Where(i => i.CartId == cartId)
            .OrderBy(i => i.AddedAt)
            .Select(i => new CartLineDto(
                i.ProductId,
                i.Product!.Name,
                i.Product.Slug,
                i.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                i.Product.Price,
                i.Product.CompareAtPrice,
                i.Quantity,
                i.Product.Stock,
                i.Product.IsActive && i.Product.Category!.IsActive && i.Product.Stock > 0))
            .ToListAsync(ct);
        return new CartDto(lines);
    }
}
