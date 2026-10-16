namespace JardinNoemi.Services.Payments;

/// <summary>
/// Contrato de pasarela de pago. Para integrar Stripe, Mercado Pago, Webpay o PayPal se implementa
/// esta interfaz y se registra en Program.cs en lugar de <see cref="MockPaymentGateway"/>.
///
/// Importante: las pasarelas reales tokenizan la tarjeta en el navegador (Stripe Elements,
/// Mercado Pago Bricks, etc.) y el servidor solo recibe un <see cref="PaymentRequest.PaymentToken"/>.
/// Nunca se deben transmitir ni almacenar números de tarjeta en este servidor.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Nombre que se muestra al cliente y se guarda en el pedido.</summary>
    string ProviderName { get; }

    /// <summary>true si la pasarela NO procesa dinero real (se informa claramente en la interfaz).</summary>
    bool IsTestMode { get; }

    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
}

public sealed record PaymentRequest(
    string OrderNumber,
    decimal Amount,
    string Currency,
    string CustomerEmail,
    string PaymentToken,
    string Description);

public enum PaymentOutcome
{
    Approved,
    Declined,
    Error
}

public sealed record PaymentResult(PaymentOutcome Outcome, string? TransactionId, string? Message)
{
    public bool IsApproved => Outcome == PaymentOutcome.Approved;
}

/// <summary>
/// Solo para pasarelas de prueba: convierte una tarjeta de prueba en un token simulado.
/// Las pasarelas reales no implementan esto; el token llega desde su SDK en el navegador.
/// </summary>
public interface ITestCardTokenizer
{
    string Tokenize(string? cardNumber);
}
