namespace JardinNoemi.DTOs;

public sealed record CartLineDto(
    int ProductId,
    string Name,
    string Slug,
    string? ImageUrl,
    decimal UnitPrice,
    decimal? CompareAtPrice,
    int Quantity,
    int Stock,
    bool IsAvailable)
{
    public decimal LineTotal => UnitPrice * Quantity;
    public bool ExceedsStock => Quantity > Stock;
}

public sealed record CartDto(IReadOnlyList<CartLineDto> Lines)
{
    public static readonly CartDto Empty = new([]);

    public int ItemCount => Lines.Sum(l => l.Quantity);
    public decimal Subtotal => Lines.Where(l => l.IsAvailable).Sum(l => l.LineTotal);
    public bool IsEmpty => Lines.Count == 0;

    /// <summary>El carrito puede pagarse solo si todas las líneas están disponibles y con stock suficiente.</summary>
    public bool CanCheckout => !IsEmpty && Lines.All(l => l.IsAvailable && !l.ExceedsStock);
}

public sealed record CartResult(bool Success, string? Message, CartDto Cart)
{
    public static CartResult Ok(CartDto cart, string? message = null) => new(true, message, cart);
    public static CartResult Fail(CartDto cart, string message) => new(false, message, cart);
}

public sealed record ShippingQuote(decimal Cost, bool IsFree, decimal AmountForFreeShipping, int DaysMin, int DaysMax);
