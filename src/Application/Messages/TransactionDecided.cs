using Domain.Entities.Sales;
using Domain.Enums;

namespace Application.Messages;

/// <summary>Published when the fraud engine has reached a decision; consumed by the BookStore to update purchase status.</summary>
public sealed record TransactionDecided(
    Guid TransactionId,
    Outcome Outcome,
    string CorrelationId,
    DateTime DecidedAt,
    int Score = 0,
    string DecidedBy = "ENGINE",
    IReadOnlyList<FraudTriggeredRule>? TriggeredRules = null);
