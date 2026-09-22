using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Rules;

/// <summary>Immutable snapshot of signals used by fraud rules. Rules only read; the use case is responsible for computing historical aggregates.</summary>
public sealed record FraudContext(
    Money Amount,
    DeliveryType Delivery,
    int ItemCount,
    Channel Channel,
    DateTime OccurredAt,
    bool IsNewCustomer,
    int RecentTransactionsWithSameCard,
    decimal CustomerAverageAmount,
    int CustomerTransactionCount,
    int RecentJustBelowThresholdCount);
