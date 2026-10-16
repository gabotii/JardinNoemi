using JardinNoemi.Models;

namespace JardinNoemi.Services;

/// <summary>Textos visibles de los enums de pedido (centralizados para mantener consistencia).</summary>
public static class OrderText
{
    public static string Status(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendiente",
        OrderStatus.Confirmed => "Confirmado",
        OrderStatus.Preparing => "En preparación",
        OrderStatus.Shipped => "Despachado",
        OrderStatus.Delivered => "Entregado",
        OrderStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    public static string Payment(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Pago pendiente",
        PaymentStatus.AwaitingTransfer => "Esperando transferencia",
        PaymentStatus.Paid => "Pagado",
        PaymentStatus.Failed => "Pago fallido",
        PaymentStatus.Refunded => "Reembolsado",
        _ => status.ToString()
    };

    public static string Method(PaymentMethod method) => method switch
    {
        PaymentMethod.Card => "Tarjeta",
        PaymentMethod.BankTransfer => "Transferencia",
        _ => method.ToString()
    };

    public static string Delivery(DeliveryMethod method) => method switch
    {
        DeliveryMethod.HomeDelivery => "Despacho a domicilio",
        DeliveryMethod.StorePickup => "Retiro en taller",
        _ => method.ToString()
    };

    public static string Care(CareLevel? level) => level switch
    {
        CareLevel.MuyFacil => "Muy fácil",
        CareLevel.Facil => "Fácil",
        CareLevel.Intermedio => "Intermedio",
        CareLevel.Experto => "Experto",
        _ => "—"
    };

    public static string AddressLine1(ShippingAddress a) =>
        string.IsNullOrWhiteSpace(a.Apartment) ? a.Street : $"{a.Street}, {a.Apartment}";

    public static string AddressLine2(ShippingAddress a) =>
        string.IsNullOrWhiteSpace(a.PostalCode) ? $"{a.City}, {a.Region}" : $"{a.City}, {a.Region} ({a.PostalCode})";

    /// <summary>Variante visual (clase CSS) del estado.</summary>
    public static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Delivered or OrderStatus.Shipped => "success",
        OrderStatus.Confirmed or OrderStatus.Preparing => "info",
        OrderStatus.Cancelled => "danger",
        _ => "warning"
    };

    public static string PaymentTone(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "success",
        PaymentStatus.Failed => "danger",
        PaymentStatus.Refunded => "neutral",
        _ => "warning"
    };
}
