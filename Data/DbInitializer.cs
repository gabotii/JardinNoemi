using System.Text.Json;
using System.Text.Json.Serialization;
using JardinNoemi.Models;
using JardinNoemi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JardinNoemi.Data;

public static class Roles
{
    public const string Admin = "Admin";
}

/// <summary>
/// Aplica migraciones y carga datos iniciales: rol Admin, usuario administrador (solo si se configuran
/// credenciales vía user-secrets / variables de entorno) y el catálogo de ejemplo desde Data/Seed/catalog.json.
/// </summary>
public static class DbInitializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var options = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (options.ApplyMigrationsOnStartup)
        {
            var dataSource = db.Database.GetDbConnection().DataSource;
            if (db.Database.IsSqlite() && !string.IsNullOrEmpty(dataSource))
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }

            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas.");
        }

        await SeedRolesAndAdminAsync(services, app.Configuration, logger);

        if (options.SeedSampleData && !await db.Categories.AnyAsync())
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "catalog.json");
            await SeedCatalogAsync(db, path, logger);
        }
    }

    private static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

        // Nunca hay credenciales en el código: se leen de user-secrets o variables de entorno.
        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Seed:AdminEmail / Seed:AdminPassword no configurados: se omite la creación del administrador.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Noemí",
                LastName = "Administradora"
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("No se pudo crear el administrador: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Usuario administrador creado: {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db, string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("No se encontró el catálogo de ejemplo en {Path}", path);
            return;
        }

        await using var stream = File.OpenRead(path);
        var seed = await JsonSerializer.DeserializeAsync<CatalogSeed>(stream, JsonOptions)
                   ?? throw new InvalidOperationException("catalog.json vacío o inválido.");

        var categories = seed.Categories.Select(c => new Category
        {
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            SortOrder = c.SortOrder,
            IsActive = true
        }).ToDictionary(c => c.Slug);

        db.Categories.AddRange(categories.Values);

        var now = DateTime.UtcNow;
        var index = 0;
        foreach (var p in seed.Products)
        {
            if (!categories.TryGetValue(p.Category, out var category))
            {
                throw new InvalidOperationException($"El producto '{p.Slug}' referencia la categoría inexistente '{p.Category}'.");
            }

            // Si el JSON no define imágenes se usa la convención /images/products/{slug}-{n}.svg
            var images = p.Images is { Count: > 0 }
                ? p.Images
                : [$"/images/products/{p.Slug}-1.svg", $"/images/products/{p.Slug}-2.svg", $"/images/products/{p.Slug}-3.svg"];
            string[] altSuffixes = ["", ", vista cercana", ", detalle"];

            db.Products.Add(new Product
            {
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                ShortDescription = p.ShortDescription,
                Description = p.Description,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                Stock = p.Stock,
                IsActive = true,
                IsFeatured = p.IsFeatured,
                Rating = p.Rating,
                ReviewCount = p.ReviewCount,
                Light = p.Light,
                Watering = p.Watering,
                Size = p.Size,
                CareLevel = p.CareLevel,
                IsPetFriendly = p.IsPetFriendly,
                Category = category,
                // Fechas escalonadas para que el orden "Novedades" sea estable.
                CreatedAt = now.AddMinutes(-index),
                UpdatedAt = now,
                Images = images.Select((url, i) => new ProductImage
                {
                    Url = url,
                    AltText = p.Name + (i < altSuffixes.Length ? altSuffixes[i] : $", imagen {i + 1}"),
                    SortOrder = i
                }).ToList()
            });
            index++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Catálogo de ejemplo cargado: {Categories} categorías, {Products} productos.", categories.Count, seed.Products.Count);
    }

    private sealed record CatalogSeed(List<CategorySeed> Categories, List<ProductSeed> Products);

    private sealed record CategorySeed(string Name, string Slug, string? Description, string? ImageUrl, int SortOrder);

    private sealed record ProductSeed(
        string Name,
        string Slug,
        string Sku,
        string Category,
        string ShortDescription,
        string Description,
        decimal Price,
        decimal? CompareAtPrice,
        int Stock,
        double Rating,
        int ReviewCount,
        bool IsFeatured,
        string? Light,
        string? Watering,
        string? Size,
        CareLevel? CareLevel,
        bool? IsPetFriendly,
        List<string>? Images);
}
