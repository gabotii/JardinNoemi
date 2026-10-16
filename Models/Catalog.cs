namespace JardinNoemi.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Product> Products { get; set; } = [];
}

public enum CareLevel
{
    MuyFacil = 1,
    Facil = 2,
    Intermedio = 3,
    Experto = 4
}

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Sku { get; set; }
    public required string ShortDescription { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Precio de venta vigente.</summary>
    public decimal Price { get; set; }

    /// <summary>Precio anterior ("antes"); si es mayor que <see cref="Price"/> el producto está en oferta.</summary>
    public decimal? CompareAtPrice { get; set; }

    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    // Ficha de cuidados (opcional: no aplica a accesorios).
    public string? Light { get; set; }
    public string? Watering { get; set; }
    public string? Size { get; set; }
    public CareLevel? CareLevel { get; set; }
    public bool? IsPetFriendly { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public List<ProductImage> Images { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsOnSale => CompareAtPrice is { } before && before > Price;
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public required string Url { get; set; }
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
