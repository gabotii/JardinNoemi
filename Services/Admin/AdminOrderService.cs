using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;

namespace JardinNoemi.Services.Admin;

public interface IAdminOrderService
{
    Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default);
    Task<PagedResult<AdminOrderListItem>> GetOrdersAsync(AdminQuery query, CancellationToken ct = default);
    Task<OrderDetailDto?> GetOrderAsync(int id, CancellationToken ct = default);
    Task<OperationResult> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default);
    Task<OperationResult> UpdatePaymentStatusAsync(int id, PaymentStatus status, CancellationToken ct = default);
    Task<PagedResult<AdminCustomerListItem>> GetCustomersAsync(AdminQuery query, CancellationToken ct = default);
}

public sealed class AdminOrderService(IDbContextFactory<ApplicationDbContext> dbFactory) : IAdminOrderService
{
    /// <summary>Transiciones de estado permitidas (flujo operativo del taller).</summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus current) => AllowedTransitions[current];

    public async Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var since = DateTime.UtcNow.AddDays(-30);

        var recentPaid = db.Orders.Where(o => o.CreatedAt >= since && o.PaymentStatus == PaymentStatus.Paid);
        var revenue = await recentPaid.SumAsync(o => (decimal?)o.Total, ct) ?? 0;
        var orders30 = await db.Orders.CountAsync(o => o.CreatedAt >= since && o.Status != OrderStatus.Cancelled, ct);
        var pending = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing, ct);
        var lowStock = await db.Products.CountAsync(p => p.IsActive && p.Stock > 0 && p.Stock <= AdminCatalogService.LowStockThreshold, ct);
        var outOfStock = await db.Products.CountAsync(p => p.IsActive && p.Stock == 0, ct);
        var customers = await db.Customers.CountAsync(ct);
        var subscribers = await db.NewsletterSubscribers.CountAsync(ct);
        var unread = await db.ContactMessages.CountAsync(m => !m.IsRead, ct);

        var recent = await ProjectOrders(db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).Take(6)).ToListAsync(ct);
        var lowList = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= AdminCatalogService.LowStockThreshold)
            .OrderBy(p => p.Stock).ThenBy(p => p.Name).Take(6)
            .Select(p => new InventoryItem(p.Id, p.Name, p.Sku, p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(), p.Stock, p.IsActive))
            .ToListAsync(ct);

        return new DashboardStats(revenue, orders30, pending, lowStock, outOfStock, customers, subscribers, unread, recent, lowList);
    }

    public async Task<PagedResult<AdminOrderListItem>> GetOrdersAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            q = q.Where(o => EF.Functions.Like(o.OrderNumber, like) || EF.Functions.Like(o.CustomerName, like) || EF.Functions.Like(o.Email, like));
        }

        if (Enum.TryParse<OrderStatus>(query.Filter, out var status))
        {
            q = q.Where(o => o.Status == status);
        }

        var total = await q.CountAsync(ct);
        var items = await ProjectOrders(q.OrderByDescending(o => o.CreatedAt)
                .Skip((Math.Max(1, query.Page) - 1) * query.PageSize).Take(query.PageSize))
            .ToListAsync(ct);
        return new PagedResult<AdminOrderListItem>(items, total, Math.Max(1, query.Page), query.PageSize);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null ? null : OrderService.ToDetail(order);
    }

    public async Task<OperationResult> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return OperationResult.Fail("Pedido no encontrado.");
        if (!AllowedTransitions[order.Status].Contains(status))
            return OperationResult.Fail($"No se puede pasar de {OrderText.Status(order.Status)} a {OrderText.Status(status)}.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (status == OrderStatus.Cancelled)
        {
            // Devolver stock de un pedido cancelado.
            foreach (var item in order.Items.Where(i => i.ProductId is not null))
            {
                await db.Products.Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
            }
            if (order.PaymentStatus is PaymentStatus.Pending or PaymentStatus.AwaitingTransfer)
                order.PaymentStatus = PaymentStatus.Failed;
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return OperationResult.Ok;
    }

    public async Task<OperationResult> UpdatePaymentStatusAsync(int id, PaymentStatus status, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return OperationResult.Fail("Pedido no encontrado.");
        if (order.Status == OrderStatus.Cancelled && status == PaymentStatus.Paid)
            return OperationResult.Fail("El pedido está cancelado.");

        order.PaymentStatus = status;
        if (status == PaymentStatus.Paid && order.Status == OrderStatus.Pending)
            order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok;
    }

    public async Task<PagedResult<AdminCustomerListItem>> GetCustomersAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            q = q.Where(c => EF.Functions.Like(c.Email, like) || EF.Functions.Like(c.FirstName, like) || EF.Functions.Like(c.LastName, like));
        }
        if (query.Filter == "registered") q = q.Where(c => c.UserId != null);
        if (query.Filter == "guest") q = q.Where(c => c.UserId == null);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.CreatedAt)
            .Skip((Math.Max(1, query.Page) - 1) * query.PageSize).Take(query.PageSize)
            .Select(c => new AdminCustomerListItem(
                c.Id,
                c.FirstName + " " + c.LastName,
                c.Email,
                c.Phone,
                c.UserId != null,
                c.Orders.Count(o => o.Status != OrderStatus.Cancelled),
                c.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => (decimal?)o.Total) ?? 0,
                c.CreatedAt,
                c.Orders.Max(o => (DateTime?)o.CreatedAt)))
            .ToListAsync(ct);
        return new PagedResult<AdminCustomerListItem>(items, total, Math.Max(1, query.Page), query.PageSize);
    }

    private static IQueryable<AdminOrderListItem> ProjectOrders(IQueryable<Order> q) =>
        q.Select(o => new AdminOrderListItem(
            o.Id, o.OrderNumber, o.CreatedAt, o.CustomerName, o.Email, o.Total, o.Status, o.PaymentStatus, o.PaymentMethod,
            o.Items.Sum(i => i.Quantity)));
}
