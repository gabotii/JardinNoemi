using JardinNoemi.DTOs;
using JardinNoemi.Models;
using Microsoft.Extensions.Options;

namespace JardinNoemi.Services;

public interface IShippingCalculator
{
    ShippingQuote Quote(decimal subtotal, DeliveryMethod method, string? region);
}

/// <summary>Tarifas planas por región con despacho gratis sobre un umbral. Configurable en "Shipping".</summary>
public sealed class ShippingCalculator(IOptionsMonitor<ShippingOptions> options) : IShippingCalculator
{
    public ShippingQuote Quote(decimal subtotal, DeliveryMethod method, string? region)
    {
        var o = options.CurrentValue;
        var missing = Math.Max(0, o.FreeShippingThreshold - subtotal);

        if (method == DeliveryMethod.StorePickup)
        {
            return new ShippingQuote(0, true, missing, 0, 1);
        }

        if (subtotal >= o.FreeShippingThreshold)
        {
            return new ShippingQuote(0, true, 0, o.EstimatedDaysMin, o.EstimatedDaysMax);
        }

        var rate = region is not null && o.RegionRates.TryGetValue(region, out var regional) ? regional : o.DefaultRate;
        return new ShippingQuote(rate, false, missing, o.EstimatedDaysMin, o.EstimatedDaysMax);
    }
}
