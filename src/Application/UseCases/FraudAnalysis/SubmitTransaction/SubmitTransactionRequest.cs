using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.FraudAnalysis.SubmitTransaction;

public sealed record SubmitTransactionRequest(
    string? ExternalReference,
    string CustomerId,
    decimal AmountValue,
    string AmountCurrency,
    string PaymentType,
    string PaymentFingerprint,
    string? PaymentLast4,
    string Channel,
    string Delivery,
    int ItemCount,
    DateTime OccurredAt,
    string CorrelationId)
    : ICommand<OperationResult<SubmitTransactionResponse>>;
