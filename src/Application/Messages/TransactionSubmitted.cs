namespace Application.Messages;

/// <summary>Published when a fraud transaction is created; consumed by the AssessTransaction worker.</summary>
public sealed record TransactionSubmitted(
    Guid TransactionId,
    string? ExternalReference,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Channel,
    string DeliveryType,
    string PaymentType,
    string PaymentFingerprint,
    string? PaymentLast4,
    int ItemCount,
    DateTime OccurredAt,
    string CorrelationId,
    DateTime SubmittedAt);
