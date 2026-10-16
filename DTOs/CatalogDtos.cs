using JardinNoemi.Models;

namespace JardinNoemi.DTOs;

public sealed record CategoryDto(int Id, string Name, string Slug, string? Description, string? ImageUrl, int ProductCount);

public sealed record ProductCardDto(
    int Id,
    string Name,
    string Slug,
    string ShortDescription,
    decimal Price,
    decimal? CompareAtPrice,
    string? ImageUrl,
    string? ImageAlt,
    double Rating,
    int ReviewCount,
    string CategoryName,
    string CategorySlug,
    int Stock)
{
    public bool InStock => Stock > 0;
    public bool IsLowStock => Stock is > 0 and <= 5;
    public bool IsOnSale => CompareAtPrice is { } before && before > Price;
    public int DiscountPercent => IsOnSale ? (int)Math.Round((1 - Price / CompareAtPrice!.Value) * 100) : 0;
}

public sealed record ProductImageDto(string Url, string AltText);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string Sku,
    string ShortDescription,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    double Rating,
    int ReviewCount,
    string? Light,
    string? Watering,
    string? Size,
    CareLevel? CareLevel,
    bool? IsPetFriendly,
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<ProductImageDto> Images)
{
    public bool InStock => Stock > 0;
    public bool IsLowStock => Stock is > 0 and <= 5;
    public bool IsOnSale => CompareAtPrice is { } before && before > Price;
    public int DiscountPercent => IsOnSale ? (int)Math.Round((1 - Price / CompareAtPrice!.Value) * 100) : 0;
    public bool HasCareInfo => Light is not null || Watering is not null || CareLevel is not null;
}

public enum ProductSort
{
    Featured,
    Newest,
    PriceAsc,
    PriceDesc,
    Rating,
    Name
}

public sealed record ProductQuery
{
    public string? Search { get; init; }
    public string? CategorySlug { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool OnSaleOnly { get; init; }
    public bool InStockOnly { get; init; }
    public bool PetFriendlyOnly { get; init; }
    public ProductSort Sort { get; init; } = ProductSort.Featured;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Empty(int pageSize = 12) => new([], 0, 1, pageSize);
}

public sealed record PriceRange(decimal Min, decimal Max);
