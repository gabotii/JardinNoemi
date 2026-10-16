using System.ComponentModel.DataAnnotations;
using JardinNoemi.Models;

namespace JardinNoemi.DTOs;

public sealed record DashboardStats(
    decimal RevenueLast30Days,
    int OrdersLast30Days,
    int PendingOrders,
    int LowStockProducts,
    int OutOfStockProducts,
    int Customers,
    int NewsletterSubscribers,
    int UnreadMessages,
    IReadOnlyList<AdminOrderListItem> RecentOrders,
    IReadOnlyList<InventoryItem> LowStock);

public sealed record AdminOrderListItem(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    string CustomerName,
    string Email,
    decimal Total,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    int ItemCount);

public sealed record AdminProductListItem(
    int Id,
    string Name,
    string Slug,
    string Sku,
    string CategoryName,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    bool IsActive,
    bool IsFeatured,
    string? ImageUrl);

public sealed record InventoryItem(int ProductId, string Name, string Sku, string? ImageUrl, int Stock, bool IsActive);

public sealed record AdminCustomerListItem(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    bool IsRegistered,
    int OrderCount,
    decimal TotalSpent,
    DateTime CreatedAt,
    DateTime? LastOrderAt);

public sealed record AdminQuery(string? Search = null, int Page = 1, int PageSize = 20, string? Filter = null);

public sealed class ProductEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [StringLength(140)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Solo minúsculas, números y guiones (p. ej. monstera-deliciosa).")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "La descripción corta es obligatoria.")]
    [StringLength(240)]
    public string ShortDescription { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100_000_000, ErrorMessage = "Precio inválido.")]
    public decimal Price { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Precio inválido.")]
    public decimal? CompareAtPrice { get; set; }

    [Range(0, 100_000, ErrorMessage = "Stock inválido.")]
    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    [StringLength(80)] public string? Light { get; set; }
    [StringLength(80)] public string? Watering { get; set; }
    [StringLength(80)] public string? Size { get; set; }
    public CareLevel? CareLevel { get; set; }
    public bool? IsPetFriendly { get; set; }

    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    public List<ProductImageEdit> Images { get; set; } = [];
}

public sealed class ProductImageEdit
{
    [Required, StringLength(400)]
    public string Url { get; set; } = string.Empty;

    [StringLength(200)]
    public string AltText { get; set; } = string.Empty;
}

public sealed class CategoryEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El slug es obligatorio.")]
    [StringLength(100)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Solo minúsculas, números y guiones.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [StringLength(400)]
    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
}

public sealed record OperationResult(bool Success, string? Error = null)
{
    public static readonly OperationResult Ok = new(true);
    public static OperationResult Fail(string error) => new(false, error);
}
