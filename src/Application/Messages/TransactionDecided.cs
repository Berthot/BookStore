namespace Application.Messages;

/// <summary>Published when the fraud engine has reached a decision; consumed by the BookStore to update purchase status.</summary>
public sealed record TransactionDecided(
    Guid TransactionId,
    string Outcome,
    string CorrelationId,
    DateTime DecidedAt);
