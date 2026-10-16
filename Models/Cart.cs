namespace JardinNoemi.Models;

/// <summary>
/// Carrito persistido en base de datos. Los visitantes anónimos se identifican por el Id del carrito
/// (guardado cifrado en el navegador); los clientes registrados por <see cref="UserId"/>.
/// </summary>
public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CartItem> Items { get; set; } = [];
}

public class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
