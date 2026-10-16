using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JardinNoemi.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto?> GetCategoryAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetFeaturedAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetOnSaleAsync(int count, CancellationToken ct = default);
    Task<PagedResult<ProductCardDto>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetRelatedAsync(int productId, int categoryId, int count, CancellationToken ct = default);
    Task<PriceRange> GetPriceRangeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(string Slug, DateTime UpdatedAt)>> GetSitemapProductsAsync(CancellationToken ct = default);
}

/// <summary>
/// Consultas de catálogo de solo lectura. Usa <see cref="IDbContextFactory{TContext}"/> (un DbContext por operación),
/// que es la práctica recomendada en Blazor Server porque los circuitos son de larga vida.
/// </summary>
public sealed class CatalogService(IDbContextFactory<ApplicationDbContext> dbFactory, IMemoryCache cache) : ICatalogService
{
    public const string CategoriesCacheKey = "catalog:categories";
    public const string PriceRangeCacheKey = "catalog:price-range";

    internal static readonly Expression<Func<Product, ProductCardDto>> ToCard = p => new ProductCardDto(
        p.Id,
        p.Name,
        p.Slug,
        p.ShortDescription,
        p.Price,
        p.CompareAtPrice,
        p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
        p.Images.OrderBy(i => i.SortOrder).Select(i => i.AltText).FirstOrDefault(),
        p.Rating,
        p.ReviewCount,
        p.Category!.Name,
        p.Category.Slug,
        p.Stock);

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            return (IReadOnlyList<CategoryDto>)await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl, c.Products.Count(p => p.IsActive)))
                .ToListAsync(ct);
        }) ?? [];
    }

    public async Task<CategoryDto?> GetCategoryAsync(string slug, CancellationToken ct = default) =>
        (await GetCategoriesAsync(ct)).FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<ProductCardDto>> GetFeaturedAsync(int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await ActiveProducts(db)
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.Stock > 0).ThenByDescending(p => p.Rating).ThenBy(p => p.Name)
            .Take(count)
            .Select(ToCard)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetOnSaleAsync(int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await ActiveProducts(db)
            .Where(p => p.CompareAtPrice != null && p.CompareAtPrice > p.Price && p.Stock > 0)
            .OrderByDescending(p => p.Rating)
            .Take(count)
            .Select(ToCard)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ProductCardDto>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 48);
        var page = Math.Max(1, query.Page);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = ActiveProducts(db);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            q = q.Where(p => p.Category!.Slug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (term.Length > 60) term = term[..60];
            var like = $"%{EscapeLike(term)}%";
            // Los slugs no tienen tildes: buscar también por el término normalizado permite
            // que "orquidea" encuentre "Orquídea".
            var slugLike = $"%{EscapeLike(Slugify(term))}%";
            q = q.Where(p =>
                EF.Functions.Like(p.Name, like, "\\") ||
                EF.Functions.Like(p.ShortDescription, like, "\\") ||
                EF.Functions.Like(p.Category!.Name, like, "\\") ||
                EF.Functions.Like(p.Slug, slugLike, "\\"));
        }

        if (query.MinPrice is { } min) q = q.Where(p => p.Price >= min);
        if (query.MaxPrice is { } max) q = q.Where(p => p.Price <= max);
        if (query.OnSaleOnly) q = q.Where(p => p.CompareAtPrice != null && p.CompareAtPrice > p.Price);
        if (query.InStockOnly) q = q.Where(p => p.Stock > 0);
        if (query.PetFriendlyOnly) q = q.Where(p => p.IsPetFriendly == true);

        q = query.Sort switch
        {
            ProductSort.Newest => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            ProductSort.PriceAsc => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ProductSort.PriceDesc => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            ProductSort.Rating => q.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount).ThenBy(p => p.Id),
            ProductSort.Name => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.Stock > 0).ThenByDescending(p => p.IsFeatured).ThenByDescending(p => p.Rating).ThenBy(p => p.Id)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(ToCard).ToListAsync(ct);
        return new PagedResult<ProductCardDto>(items, total, page, pageSize);
    }

    public async Task<ProductDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await ActiveProducts(db)
            .Where(p => p.Slug == slug)
            .Select(p => new ProductDetailDto(
                p.Id, p.Name, p.Slug, p.Sku, p.ShortDescription, p.Description, p.Price, p.CompareAtPrice, p.Stock,
                p.Rating, p.ReviewCount, p.Light, p.Watering, p.Size, p.CareLevel, p.IsPetFriendly,
                p.CategoryId, p.Category!.Name, p.Category.Slug,
                p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Url, i.AltText)).ToList()))
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ProductCardDto>> GetRelatedAsync(int productId, int categoryId, int count, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var related = await ActiveProducts(db)
            .Where(p => p.CategoryId == categoryId && p.Id != productId)
            .OrderByDescending(p => p.Stock > 0).ThenByDescending(p => p.Rating)
            .Take(count)
            .Select(ToCard)
            .ToListAsync(ct);

        if (related.Count < count)
        {
            var exclude = related.Select(r => r.Id).Append(productId).ToList();
            related.AddRange(await ActiveProducts(db)
                .Where(p => !exclude.Contains(p.Id) && p.IsFeatured && p.Stock > 0)
                .OrderByDescending(p => p.Rating)
                .Take(count - related.Count)
                .Select(ToCard)
                .ToListAsync(ct));
        }

        return related;
    }

    public async Task<PriceRange> GetPriceRangeAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(PriceRangeCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var prices = ActiveProducts(db).Select(p => p.Price);
            if (!await prices.AnyAsync(ct)) return new PriceRange(0, 0);
            return new PriceRange(await prices.MinAsync(ct), await prices.MaxAsync(ct));
        }) ?? new PriceRange(0, 0);
    }

    public async Task<IReadOnlyList<(string Slug, DateTime UpdatedAt)>> GetSitemapProductsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await ActiveProducts(db).Select(p => new { p.Slug, p.UpdatedAt }).ToListAsync(ct);
        return rows.Select(r => (r.Slug, r.UpdatedAt)).ToList();
    }

    private static IQueryable<Product> ActiveProducts(ApplicationDbContext db) =>
        db.Products.AsNoTracking().Where(p => p.IsActive && p.Category!.IsActive);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>Convierte texto a slug: minúsculas, sin tildes, palabras separadas por guion.</summary>
    public static string Slugify(string text)
    {
        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
