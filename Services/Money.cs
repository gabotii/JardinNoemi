using System.Globalization;

namespace JardinNoemi.Services;

/// <summary>Formato de montos según la cultura configurada en "Store:Culture" (por defecto es-CL, sin decimales).</summary>
public static class Money
{
    private static CultureInfo _culture = CultureInfo.GetCultureInfo("es-CL");
    private static int _decimals;

    public static CultureInfo Culture => _culture;

    public static void Configure(CultureInfo culture)
    {
        _culture = culture;
        _decimals = culture.NumberFormat.CurrencyDecimalDigits;
        // CLP no usa decimales aunque .NET reporte 0 o 2 según plataforma.
        if (culture.Name.Equals("es-CL", StringComparison.OrdinalIgnoreCase)) _decimals = 0;
    }

    public static string ToMoney(this decimal amount) => amount.ToString("C" + _decimals, _culture);

    /// <summary>Monto sin símbolo, útil para datos estructurados (schema.org).</summary>
    public static string ToInvariantAmount(this decimal amount) =>
        Math.Round(amount, _decimals).ToString("0.##", CultureInfo.InvariantCulture);
}

public static class Formatting
{
    public static string ToLocalDate(this DateTime utc) =>
        utc.ToLocalTime().ToString("d 'de' MMMM, yyyy", Money.Culture);

    public static string ToLocalDateTime(this DateTime utc) =>
        utc.ToLocalTime().ToString("dd-MM-yyyy HH:mm", Money.Culture);

    public static string Pluralize(this int count, string singular, string plural) =>
        $"{count} {(count == 1 ? singular : plural)}";
}
