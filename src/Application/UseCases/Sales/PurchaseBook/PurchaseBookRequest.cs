using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Sales.PurchaseBook;

public sealed record PurchaseBookRequest(
    Guid BookId,
    int Quantity,
    string PaymentType,
    string PaymentFingerprint,
    string? PaymentLast4,
    string CustomerId,
    string CorrelationId) : ICommand<OperationResult<PurchaseBookResponse>>;
