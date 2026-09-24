namespace Domain.Entities.Sales;

public sealed record FraudTriggeredRule(string Code, string Reason);

/// <summary>Immutable snapshot of the fraud decision stored on the purchase so BookStore.Api can return full fraudDetails without a cross-domain call.</summary>
public sealed record FraudOutcomeSnapshot(
    int Score,
    string DecidedBy,
    IReadOnlyList<FraudTriggeredRule> TriggeredRules);
