using System.Globalization;

namespace JardinNoemi.Services;

/// <summary>Información pública del negocio (appsettings: "Store").</summary>
public sealed class ShopOptions
{
    public const string SectionName = "Store";

    public string Name { get; set; } = "JardinNoemi";
    public string LegalName { get; set; } = "JardinNoemi SpA";
    public string Tagline { get; set; } = "Plantas con alma para hogares con historia";
    public string BaseUrl { get; set; } = "https://localhost:7087";
    public string Culture { get; set; } = "es-CL";
    public string CurrencyCode { get; set; } = "CLP";
    public string Email { get; set; } = "hola@jardinnoemi.cl";
    public string Phone { get; set; } = "+56 9 1234 5678";
    public string WhatsApp { get; set; } = "56912345678";
    public string Address { get; set; } = "Taller y retiro: Av. Italia 1234, Providencia, Santiago";
    public string OpeningHours { get; set; } = "Lunes a sábado, 10:00 a 19:00";
    public string Instagram { get; set; } = "https://instagram.com/jardinnoemi";
    public string Facebook { get; set; } = "https://facebook.com/jardinnoemi";
    public string TikTok { get; set; } = "https://tiktok.com/@jardinnoemi";

    /// <summary>Datos para pagos por transferencia (se muestran en la confirmación del pedido).</summary>
    public BankTransferInfo BankTransfer { get; set; } = new();

    public CultureInfo CultureInfo => CultureInfo.GetCultureInfo(Culture);
}

public sealed class BankTransferInfo
{
    public string Bank { get; set; } = "Banco de ejemplo";
    public string AccountType { get; set; } = "Cuenta corriente";
    public string AccountNumber { get; set; } = "00-000-00000-00";
    public string Holder { get; set; } = "JardinNoemi SpA";
    public string TaxId { get; set; } = "76.000.000-0";
    public string Email { get; set; } = "pagos@jardinnoemi.cl";
    public int HoldHours { get; set; } = 48;
}

/// <summary>Tarifas de despacho (appsettings: "Shipping").</summary>
public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public decimal DefaultRate { get; set; } = 5990m;
    public decimal FreeShippingThreshold { get; set; } = 49990m;
    public int EstimatedDaysMin { get; set; } = 2;
    public int EstimatedDaysMax { get; set; } = 5;
    public Dictionary<string, decimal> RegionRates { get; set; } = [];
}

/// <summary>Inicialización de base de datos (appsettings: "Database").</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Aplica migraciones al iniciar. Recomendado solo en desarrollo; en producción usar bundles/scripts.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Carga el catálogo de ejemplo si la base de datos está vacía.</summary>
    public bool SeedSampleData { get; set; }
}

/// <summary>Almacenamiento de imágenes subidas desde el panel admin (appsettings: "Storage").</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Ruta física (relativa al ContentRoot o absoluta) fuera de wwwroot.</summary>
    public string UploadsPath { get; set; } = "App_Data/uploads";
    public string RequestPath { get; set; } = "/uploads";
    public long MaxImageBytes { get; set; } = 2 * 1024 * 1024;
}
