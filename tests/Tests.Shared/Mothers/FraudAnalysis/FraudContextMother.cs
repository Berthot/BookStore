using Domain.Enums;
using Domain.Rules;
using Domain.ValueObjects;
using Tests.Shared.Constants;

namespace Tests.Shared.Mothers.FraudAnalysis;

public static class FraudContextMother
{
    public static FraudContext Default() => new(
        Amount: new Money(50m, "BRL"),
        Delivery: DeliveryType.Physical,
        ItemCount: 1,
        Channel: Channel.Web,
        OccurredAt: TestConstants.Dates.FixedUtcNow,
        IsNewCustomer: false,
        RecentTransactionsWithSameCard: 0,
        CustomerAverageAmount: 50m,
        CustomerTransactionCount: 10,
        RecentJustBelowThresholdCount: 0);

    public static FraudContext HighValueDigital() => Default() with
    {
        Amount = new Money(600m, "BRL"),
        Delivery = DeliveryType.Digital
    };

    public static FraudContext NewCustomer() => Default() with
    {
        IsNewCustomer = true,
        Amount = new Money(400m, "BRL")
    };

    public static FraudContext BulkPurchase() => Default() with { ItemCount = 10 };

    public static FraudContext CardVelocity() => Default() with { RecentTransactionsWithSameCard = 15 };
}
