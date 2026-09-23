namespace Application.UseCases.Sales.Ports;

/// <summary>Port that submits a purchase to fraud analysis. Decouples Sales from any specific fraud implementation.</summary>
public interface IFraudCheckGateway
{
    /// <summary>Submits the purchase to fraud analysis and returns the created transaction id.</summary>
    Task<Guid> SubmitAsync(
        Guid purchaseId,
        string bookFormat,
        int quantity,
        decimal totalAmount,
        string currency,
        string paymentType,
        string paymentFingerprint,
        string? paymentLast4,
        string customerId,
        string correlationId,
        DateTime placedAt,
        CancellationToken cancellationToken = default);
}
