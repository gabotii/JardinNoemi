using System.ComponentModel.DataAnnotations;
using JardinNoemi.Models;

namespace JardinNoemi.DTOs;

/// <summary>
/// Formulario de checkout. Se valida en el cliente (EditForm + DataAnnotations) y se vuelve a validar
/// completo en el servidor (<see cref="Services.OrderService"/>) antes de crear el pedido.
/// </summary>
public sealed class CheckoutForm
{
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu apellido.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa un teléfono de contacto.")]
    [RegularExpression(@"^\+?[0-9\s\-()]{8,20}$", ErrorMessage = "Ingresa un teléfono válido, p. ej. +56 9 1234 5678.")]
    public string Phone { get; set; } = string.Empty;

    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

    [RequiredWhen(nameof(DeliveryMethod), DeliveryMethod.HomeDelivery, ErrorMessage = "Ingresa la dirección de despacho.")]
    [StringLength(160, ErrorMessage = "Máximo 160 caracteres.")]
    public string? Street { get; set; }

    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string? Apartment { get; set; }

    [RequiredWhen(nameof(DeliveryMethod), DeliveryMethod.HomeDelivery, ErrorMessage = "Ingresa la comuna o ciudad.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string? City { get; set; }

    [RegionWhen(nameof(DeliveryMethod), DeliveryMethod.HomeDelivery, ErrorMessage = "Selecciona una región.")]
    public string? Region { get; set; }

    [RegularExpression(@"^[0-9A-Za-z\- ]{3,10}$", ErrorMessage = "Código postal inválido.")]
    public string? PostalCode { get; set; }

    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string? Notes { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;

    // --- Datos de tarjeta: solo para la pasarela de PRUEBA. Nunca se persisten ni se registran en logs. ---
    [RequiredWhen(nameof(PaymentMethod), PaymentMethod.Card, ErrorMessage = "Ingresa el nombre que aparece en la tarjeta.")]
    [StringLength(80)]
    public string? CardName { get; set; }

    [CardNumberWhen(nameof(PaymentMethod), PaymentMethod.Card, ErrorMessage = "Número de tarjeta inválido.")]
    public string? CardNumber { get; set; }

    [CardExpiryWhen(nameof(PaymentMethod), PaymentMethod.Card, ErrorMessage = "Vencimiento inválido (usa MM/AA).")]
    public string? CardExpiry { get; set; }

    [CvcWhen(nameof(PaymentMethod), PaymentMethod.Card, ErrorMessage = "CVC inválido.")]
    public string? CardCvc { get; set; }

    /// <summary>Token entregado por el SDK de una pasarela real (tokenización en el navegador).</summary>
    public string? PaymentToken { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los términos y condiciones.")]
    public bool AcceptTerms { get; set; }
}

public static class CardValidation
{
    public static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }

    /// <summary>Interpreta "MM/AA" y devuelve el último día de ese mes.</summary>
    public static bool TryParseExpiry(string? value, out DateOnly lastDay)
    {
        lastDay = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Replace(" ", string.Empty).Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var year)) return false;
        if (month is < 1 or > 12 || year is < 0 or > 99) return false;
        var fullYear = 2000 + year;
        lastDay = new DateOnly(fullYear, month, DateTime.DaysInMonth(fullYear, month));
        return true;
    }
}

public sealed record PlaceOrderResult(bool Success, string? OrderNumber, string? AccessToken, string? ErrorMessage, bool CartChanged = false)
{
    public static PlaceOrderResult Fail(string message, bool cartChanged = false) => new(false, null, null, message, cartChanged);
}

public sealed record OrderLineDto(string ProductName, string ProductSlug, string? ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderDetailDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    string? PaymentProvider,
    string? PaymentReference,
    DeliveryMethod DeliveryMethod,
    string CustomerName,
    string Email,
    string Phone,
    ShippingAddress? ShippingAddress,
    string? Notes,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record OrderSummaryDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    int ItemCount,
    string? FirstImageUrl);

public sealed class ContactForm
{
    [Required(ErrorMessage = "Cuéntanos tu nombre.")]
    [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Necesitamos tu correo para responderte.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9\s\-()]{8,20}$", ErrorMessage = "Ingresa un teléfono válido.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Escribe tu mensaje.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "El mensaje debe tener entre 10 y 2000 caracteres.")]
    public string Message { get; set; } = string.Empty;
}

public sealed class NewsletterForm
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfileForm
{
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu apellido.")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9\s\-()]{8,20}$", ErrorMessage = "Ingresa un teléfono válido.")]
    public string? Phone { get; set; }

    [StringLength(160)]
    public string? Street { get; set; }

    [StringLength(60)]
    public string? Apartment { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    public string? Region { get; set; }

    [RegularExpression(@"^[0-9A-Za-z\- ]{3,10}$", ErrorMessage = "Código postal inválido.")]
    public string? PostalCode { get; set; }
}

public sealed record CustomerProfileDto(
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? Street,
    string? Apartment,
    string? City,
    string? Region,
    string? PostalCode);
