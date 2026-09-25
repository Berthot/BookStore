using Domain.Enums;

namespace Application.UseCases.Sales.GetPurchase;

/// <summary>Fraud analysis details for a purchase (D-40: separate public message from internal audit trail).</summary>
public sealed record FraudDetailsResponse(
    Guid? TransactionId,
    Outcome? Outcome,
    int? Score,
    string? DecidedBy,
    IReadOnlyList<TriggeredRuleSummary>? TriggeredRules);

/// <summary>Rule that fired during fraud evaluation, shown for auditability (D-40).</summary>
public sealed record TriggeredRuleSummary(string Code, string Reason);

public sealed record GetPurchaseResponse(
    Guid PurchaseId,
    PurchaseStatus Status,
    string CustomerMessage,
    FraudDetailsResponse? FraudDetails,
    DateTime CreatedAt);
