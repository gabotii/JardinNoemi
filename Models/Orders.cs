namespace JardinNoemi.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    Pending = 0,
    AwaitingTransfer = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum PaymentMethod
{
    Card = 0,
    BankTransfer = 1
}

public enum DeliveryMethod
{
    HomeDelivery = 0,
    StorePickup = 1
}

public class Order
{
    public int Id { get; set; }

    /// <summary>Número visible para el cliente, p. ej. JN-260928-K7QX.</summary>
    public required string OrderNumber { get; set; }

    /// <summary>Token aleatorio que permite a un comprador invitado ver su confirmación.</summary>
    public required string AccessToken { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? UserId { get; set; }

    // Instantánea de datos de contacto al momento de la compra.
    public required string CustomerName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }

    public DeliveryMethod DeliveryMethod { get; set; }
    public ShippingAddress? ShippingAddress { get; set; }
    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}

/// <summary>Dirección de despacho copiada en el pedido (tipo propio / owned de EF Core).</summary>
public class ShippingAddress
{
    public required string Street { get; set; }
    public string? Apartment { get; set; }
    public required string City { get; set; }
    public required string Region { get; set; }
    public string? PostalCode { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>Referencia opcional: el producto puede eliminarse sin afectar el historial.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public required string ProductName { get; set; }
    public required string ProductSlug { get; set; }
    public required string Sku { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
