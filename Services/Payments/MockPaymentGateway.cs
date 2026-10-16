namespace JardinNoemi.Services.Payments;

/// <summary>
/// Pasarela SIMULADA para desarrollo y demostraciones. No contacta a ningún proveedor ni mueve dinero.
/// La interfaz lo indica explícitamente ("Modo de prueba") y el pedido queda registrado con este proveedor.
///
/// Tarjetas de prueba:
///   4242 4242 4242 4242 → aprobada
///   4000 0000 0000 0002 → rechazada
///   4000 0000 0000 0119 → error de procesamiento
/// Cualquier otra tarjeta válida (Luhn) se aprueba.
/// </summary>
public sealed class MockPaymentGateway(ILogger<MockPaymentGateway> logger) : IPaymentGateway, ITestCardTokenizer
{
    public const string DeclinedToken = "mock_tok_declined";
    public const string ErrorToken = "mock_tok_error";
    public const string ApprovedToken = "mock_tok_approved";

    public string ProviderName => "Pasarela de prueba (sin cargo real)";
    public bool IsTestMode => true;

    /// <summary>
    /// Simula la tokenización que haría el SDK del proveedor en el navegador. Solo conserva el resultado
    /// esperado de la tarjeta de prueba; el número se descarta inmediatamente.
    /// </summary>
    public string Tokenize(string? cardNumber)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits switch
        {
            "4000000000000002" => DeclinedToken,
            "4000000000000119" => ErrorToken,
            _ => ApprovedToken
        };
    }

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        // Latencia simulada para ejercitar los estados de carga de la interfaz.
        await Task.Delay(TimeSpan.FromMilliseconds(900), ct);

        logger.LogInformation("Pago SIMULADO para {OrderNumber} por {Amount} {Currency}", request.OrderNumber, request.Amount, request.Currency);

        return request.PaymentToken switch
        {
            DeclinedToken => new PaymentResult(PaymentOutcome.Declined, null,
                "Tu banco rechazó el pago (simulado). Prueba con otra tarjeta."),
            ErrorToken => new PaymentResult(PaymentOutcome.Error, null,
                "No pudimos comunicarnos con la pasarela (simulado). Inténtalo en unos minutos."),
            _ => new PaymentResult(PaymentOutcome.Approved, $"TEST-{Guid.NewGuid():N}"[..17].ToUpperInvariant(), null)
        };
    }
}
