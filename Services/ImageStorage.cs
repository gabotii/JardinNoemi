using Microsoft.Extensions.Options;

namespace JardinNoemi.Services;

/// <summary>Almacenamiento de imágenes de producto. Reemplazable por Azure Blob Storage, S3, Cloudinary, etc.</summary>
public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}

/// <summary>
/// Guarda imágenes en disco fuera de wwwroot (servidas en /uploads por un StaticFileProvider dedicado).
/// Valida tipo real por firma de bytes (no solo por extensión) y genera nombres aleatorios.
/// </summary>
public sealed class LocalImageStorage(IWebHostEnvironment env, IOptions<StorageOptions> options) : IImageStorage
{
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public string PhysicalRoot => Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.UploadsPath));

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
            throw new InvalidOperationException("Formato no permitido. Usa JPG, PNG o WebP.");

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0 || buffer.Length > options.Value.MaxImageBytes)
            throw new InvalidOperationException($"La imagen debe pesar menos de {options.Value.MaxImageBytes / 1024 / 1024} MB.");

        if (!HasValidSignature(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)), extension))
            throw new InvalidOperationException("El archivo no parece ser una imagen válida.");

        var folder = Path.Combine(PhysicalRoot, "products");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        buffer.Position = 0;
        await using (var file = File.Create(Path.Combine(folder, name)))
        {
            await buffer.CopyToAsync(file, ct);
        }

        return $"{options.Value.RequestPath.TrimEnd('/')}/products/{name}";
    }

    private static bool HasValidSignature(ReadOnlySpan<byte> header, string extension) => extension switch
    {
        ".jpg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        ".png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };
}
