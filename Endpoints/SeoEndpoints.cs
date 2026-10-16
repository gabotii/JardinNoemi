using System.Text;
using System.Xml;
using JardinNoemi.Services;
using Microsoft.Extensions.Options;

namespace JardinNoemi.Endpoints;

internal static class SeoEndpoints
{
    private static readonly string[] StaticPages = ["/", "/tienda", "/nosotros", "/contacto", "/politicas/envios", "/politicas/devoluciones", "/politicas/privacidad", "/politicas/terminos"];

    public static void MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", (IOptions<ShopOptions> store) =>
        {
            var baseUrl = store.Value.BaseUrl.TrimEnd('/');
            var body = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Allow: /")
                .AppendLine("Disallow: /admin")
                .AppendLine("Disallow: /cuenta")
                .AppendLine("Disallow: /carrito")
                .AppendLine("Disallow: /checkout")
                .AppendLine("Disallow: /pedido")
                .AppendLine($"Sitemap: {baseUrl}/sitemap.xml")
                .ToString();
            return Results.Text(body, "text/plain; charset=utf-8");
        });

        endpoints.MapGet("/sitemap.xml", async (ICatalogService catalog, IOptions<ShopOptions> store, HttpContext http, CancellationToken ct) =>
        {
            var baseUrl = store.Value.BaseUrl.TrimEnd('/');
            var categories = await catalog.GetCategoriesAsync(ct);
            var products = await catalog.GetSitemapProductsAsync(ct);

            var sb = new StringBuilder();
            await using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Async = true, Indent = true, Encoding = Encoding.UTF8 }))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var page in StaticPages) WriteUrl(writer, baseUrl + page, null, page == "/" ? "1.0" : "0.6");
                foreach (var c in categories) WriteUrl(writer, $"{baseUrl}/tienda/{c.Slug}", null, "0.8");
                foreach (var (slug, updatedAt) in products) WriteUrl(writer, $"{baseUrl}/producto/{slug}", updatedAt, "0.9");
                writer.WriteEndElement();
                await writer.WriteEndDocumentAsync();
            }

            http.Response.Headers.CacheControl = "public,max-age=3600";
            // XmlWriter sobre StringBuilder declara utf-16; se normaliza a utf-8 que es lo que se envía.
            return Results.Text(sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\""), "application/xml; charset=utf-8");
        });
    }

    private static void WriteUrl(XmlWriter writer, string loc, DateTime? lastModified, string priority)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        if (lastModified is { } lm) writer.WriteElementString("lastmod", lm.ToString("yyyy-MM-dd"));
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}
