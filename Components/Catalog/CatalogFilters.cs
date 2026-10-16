using JardinNoemi.DTOs;

namespace JardinNoemi.Components.Catalog;

/// <summary>Estado de filtros del catálogo. Se refleja 1:1 en la URL para que sea compartible e indexable.</summary>
public sealed record CatalogFilters
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool OnSale { get; init; }
    public bool InStock { get; init; }
    public bool PetFriendly { get; init; }
    public ProductSort Sort { get; init; } = ProductSort.Featured;
    public int Page { get; init; } = 1;

    public bool HasActiveFilters =>
        Search is not null || MinPrice is not null || MaxPrice is not null || OnSale || InStock || PetFriendly;

    public ProductQuery ToQuery(int pageSize) => new()
    {
        Search = Search,
        CategorySlug = Category,
        MinPrice = MinPrice,
        MaxPrice = MaxPrice,
        OnSaleOnly = OnSale,
        InStockOnly = InStock,
        PetFriendlyOnly = PetFriendly,
        Sort = Sort,
        Page = Page,
        PageSize = pageSize
    };

    /// <summary>Construye la URL pública (/tienda o /tienda/{categoria}) con los filtros como query string.</summary>
    public string ToUrl()
    {
        var path = Category is null ? "/tienda" : $"/tienda/{Category}";
        var query = new List<string>();
        if (Search is not null) query.Add($"q={Uri.EscapeDataString(Search)}");
        if (MinPrice is not null) query.Add($"min={MinPrice:0}");
        if (MaxPrice is not null) query.Add($"max={MaxPrice:0}");
        if (OnSale) query.Add("oferta=true");
        if (InStock) query.Add("stock=true");
        if (PetFriendly) query.Add("mascotas=true");
        if (Sort != ProductSort.Featured) query.Add($"orden={SortToken(Sort)}");
        if (Page > 1) query.Add($"pagina={Page}");
        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    public static string SortToken(ProductSort sort) => sort switch
    {
        ProductSort.Newest => "novedades",
        ProductSort.PriceAsc => "precio-asc",
        ProductSort.PriceDesc => "precio-desc",
        ProductSort.Rating => "valoracion",
        ProductSort.Name => "nombre",
        _ => "destacados"
    };

    public static ProductSort ParseSort(string? token) => token switch
    {
        "novedades" => ProductSort.Newest,
        "precio-asc" => ProductSort.PriceAsc,
        "precio-desc" => ProductSort.PriceDesc,
        "valoracion" => ProductSort.Rating,
        "nombre" => ProductSort.Name,
        _ => ProductSort.Featured
    };

    public static string SortLabel(ProductSort sort) => sort switch
    {
        ProductSort.Newest => "Novedades",
        ProductSort.PriceAsc => "Precio: menor a mayor",
        ProductSort.PriceDesc => "Precio: mayor a menor",
        ProductSort.Rating => "Mejor valorados",
        ProductSort.Name => "Nombre (A–Z)",
        _ => "Destacados"
    };
}
