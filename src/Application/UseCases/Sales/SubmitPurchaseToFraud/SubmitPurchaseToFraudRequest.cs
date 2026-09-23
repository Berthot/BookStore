using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Sales.SubmitPurchaseToFraud;

public sealed record SubmitPurchaseToFraudRequest(
    Guid PurchaseId,
    Guid BookId,
    int Quantity,
    decimal TotalAmount,
    string Currency,
    string BookFormat,
    string PaymentType,
    string PaymentFingerprint,
    string? PaymentLast4,
    string CustomerId,
    string CorrelationId,
    DateTime PlacedAt) : ICommand<OperationResult<SubmitPurchaseToFraudResponse>>;
