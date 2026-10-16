namespace JardinNoemi.Models;

/// <summary>
/// Regiones de despacho. Supuesto inicial: la tienda opera en Chile.
/// Para otro país basta con reemplazar esta lista (y las tarifas en appsettings "Shipping").
/// </summary>
public static class Regions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Arica y Parinacota",
        "Tarapacá",
        "Antofagasta",
        "Atacama",
        "Coquimbo",
        "Valparaíso",
        "Metropolitana de Santiago",
        "Libertador General Bernardo O'Higgins",
        "Maule",
        "Ñuble",
        "Biobío",
        "La Araucanía",
        "Los Ríos",
        "Los Lagos",
        "Aysén del General Carlos Ibáñez del Campo",
        "Magallanes y de la Antártica Chilena"
    ];

    public static bool IsValid(string? region) => region is not null && All.Contains(region);
}
