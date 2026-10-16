using JardinNoemi.Data;
using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JardinNoemi.Services.Admin;

public interface IAdminCatalogService
{
    Task<PagedResult<AdminProductListItem>> GetProductsAsync(AdminQuery query, CancellationToken ct = default);
    Task<ProductEditModel?> GetProductForEditAsync(int id, CancellationToken ct = default);
    Task<OperationResult> SaveProductAsync(ProductEditModel model, CancellationToken ct = default);
    Task<OperationResult> SetProductActiveAsync(int id, bool isActive, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryEditModel>> GetCategoriesAsync(CancellationToken ct = default);
    Task<OperationResult> SaveCategoryAsync(CategoryEditModel model, CancellationToken ct = default);
    Task<OperationResult> DeleteCategoryAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string? search, bool lowStockOnly, CancellationToken ct = default);
    Task<OperationResult> UpdateStockAsync(int productId, int stock, CancellationToken ct = default);
}

public sealed class AdminCatalogService(IDbContextFactory<ApplicationDbContext> dbFactory, IMemoryCache cache) : IAdminCatalogService
{
    public const int LowStockThreshold = 5;

    public async Task<PagedResult<AdminProductListItem>> GetProductsAsync(AdminQuery query, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            q = q.Where(p => EF.Functions.Like(p.Name, like) || EF.Functions.Like(p.Sku, like));
        }

        q = query.Filter switch
        {
            "active" => q.Where(p => p.IsActive),
            "inactive" => q.Where(p => !p.IsActive),
            "featured" => q.Where(p => p.IsFeatured),
            "sale" => q.Where(p => p.CompareAtPrice != null && p.CompareAtPrice > p.Price),
            "out" => q.Where(p => p.Stock == 0),
            _ => q
        };

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.Category!.SortOrder).ThenBy(p => p.Name)
            .Skip((Math.Max(1, query.Page) - 1) * query.PageSize).Take(query.PageSize)
            .Select(p => new AdminProductListItem(
                p.Id, p.Name, p.Slug, p.Sku, p.Category!.Name, p.Price, p.CompareAtPrice, p.Stock, p.IsActive, p.IsFeatured,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(ct);
        return new PagedResult<AdminProductListItem>(items, total, Math.Max(1, query.Page), query.PageSize);
    }

    public async Task<ProductEditModel?> GetProductForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var p = await db.Products.AsNoTracking().Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        return new ProductEditModel
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            CategoryId = p.CategoryId,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            Price = p.Price,
            CompareAtPrice = p.CompareAtPrice,
            Stock = p.Stock,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured,
            Light = p.Light,
            Watering = p.Watering,
            Size = p.Size,
            CareLevel = p.CareLevel,
            IsPetFriendly = p.IsPetFriendly,
            Rating = p.Rating,
            ReviewCount = p.ReviewCount,
            Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageEdit { Url = i.Url, AltText = i.AltText }).ToList()
        };
    }

    public async Task<OperationResult> SaveProductAsync(ProductEditModel model, CancellationToken ct = default)
    {
        if (model.CompareAtPrice is { } before && before > 0 && before <= model.Price)
            return OperationResult.Fail("El precio anterior debe ser mayor que el precio de venta (o dejarse vacío).");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (await db.Products.AnyAsync(p => p.Slug == model.Slug && p.Id != model.Id, ct))
            return OperationResult.Fail("Ya existe un producto con ese slug.");
        if (await db.Products.AnyAsync(p => p.Sku == model.Sku && p.Id != model.Id, ct))
            return OperationResult.Fail("Ya existe un producto con ese SKU.");
        if (!await db.Categories.AnyAsync(c => c.Id == model.CategoryId, ct))
            return OperationResult.Fail("La categoría seleccionada no existe.");

        Product product;
        if (model.Id == 0)
        {
            product = new Product { Name = model.Name, Slug = model.Slug, Sku = model.Sku, ShortDescription = model.ShortDescription };
            db.Products.Add(product);
        }
        else
        {
            product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == model.Id, ct)
                      ?? throw new InvalidOperationException("Producto no encontrado.");
        }

        product.Name = model.Name.Trim();
        product.Slug = model.Slug.Trim();
        product.Sku = model.Sku.Trim().ToUpperInvariant();
        product.CategoryId = model.CategoryId;
        product.ShortDescription = model.ShortDescription.Trim();
        product.Description = model.Description?.Trim() ?? string.Empty;
        product.Price = model.Price;
        product.CompareAtPrice = model.CompareAtPrice is > 0 ? model.CompareAtPrice : null;
        product.Stock = model.Stock;
        product.IsActive = model.IsActive;
        product.IsFeatured = model.IsFeatured;
        product.Light = NullIfEmpty(model.Light);
        product.Watering = NullIfEmpty(model.Watering);
        product.Size = NullIfEmpty(model.Size);
        product.CareLevel = model.CareLevel;
        product.IsPetFriendly = model.IsPetFriendly;
        product.UpdatedAt = DateTime.UtcNow;

        product.Images.Clear();
        var order = 0;
        foreach (var image in model.Images.Where(i => !string.IsNullOrWhiteSpace(i.Url)))
        {
            if (!IsSafeImageUrl(image.Url)) return OperationResult.Fail($"URL de imagen no permitida: {image.Url}");
            product.Images.Add(new ProductImage
            {
                Url = image.Url.Trim(),
                AltText = string.IsNullOrWhiteSpace(image.AltText) ? product.Name : image.AltText.Trim(),
                SortOrder = order++
            });
        }

        await db.SaveChangesAsync(ct);
        model.Id = product.Id;
        InvalidateCatalogCache();
        return OperationResult.Ok;
    }

    public async Task<OperationResult> SetProductActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var updated = await db.Products.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, isActive).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        InvalidateCatalogCache();
        return updated == 1 ? OperationResult.Ok : OperationResult.Fail("Producto no encontrado.");
    }

    public async Task<IReadOnlyList<CategoryEditModel>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryEditModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count
            })
            .ToListAsync(ct);
    }

    public async Task<OperationResult> SaveCategoryAsync(CategoryEditModel model, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(model.ImageUrl) && !IsSafeImageUrl(model.ImageUrl))
            return OperationResult.Fail("URL de imagen no permitida.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Categories.AnyAsync(c => c.Slug == model.Slug && c.Id != model.Id, ct))
            return OperationResult.Fail("Ya existe una categoría con ese slug.");

        Category category;
        if (model.Id == 0)
        {
            category = new Category { Name = model.Name, Slug = model.Slug };
            db.Categories.Add(category);
        }
        else
        {
            category = await db.Categories.FindAsync([model.Id], ct) ?? throw new InvalidOperationException("Categoría no encontrada.");
        }

        category.Name = model.Name.Trim();
        category.Slug = model.Slug.Trim();
        category.Description = NullIfEmpty(model.Description);
        category.ImageUrl = NullIfEmpty(model.ImageUrl);
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;

        await db.SaveChangesAsync(ct);
        model.Id = category.Id;
        InvalidateCatalogCache();
        return OperationResult.Ok;
    }

    public async Task<OperationResult> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            return OperationResult.Fail("No puedes eliminar una categoría con productos. Muévelos o desactívala.");

        await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        InvalidateCatalogCache();
        return OperationResult.Ok;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(string? search, bool lowStockOnly, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            q = q.Where(p => EF.Functions.Like(p.Name, like) || EF.Functions.Like(p.Sku, like));
        }
        if (lowStockOnly) q = q.Where(p => p.Stock <= LowStockThreshold);

        return await q.OrderBy(p => p.Stock).ThenBy(p => p.Name)
            .Select(p => new InventoryItem(p.Id, p.Name, p.Sku, p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(), p.Stock, p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<OperationResult> UpdateStockAsync(int productId, int stock, CancellationToken ct = default)
    {
        if (stock is < 0 or > 100_000) return OperationResult.Fail("El stock debe estar entre 0 y 100.000.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var updated = await db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, stock).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        return updated == 1 ? OperationResult.Ok : OperationResult.Fail("Producto no encontrado.");
    }

    private void InvalidateCatalogCache()
    {
        cache.Remove(CatalogService.CategoriesCacheKey);
        cache.Remove(CatalogService.PriceRangeCacheKey);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Solo rutas locales o https: evita javascript:, data: y otros esquemas peligrosos.</summary>
    private static bool IsSafeImageUrl(string url)
    {
        url = url.Trim();
        if (url.StartsWith('/') && !url.StartsWith("//")) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
