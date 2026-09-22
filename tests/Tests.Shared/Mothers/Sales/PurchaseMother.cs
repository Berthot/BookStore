using Domain.Entities.Sales;
using Domain.Enums;
using Domain.ValueObjects;
using Tests.Shared.Constants;

namespace Tests.Shared.Mothers.Sales;

public static class PurchaseMother
{
    public static Purchase PendingFraudCheck() => new PurchaseBuilder().Build();

    public static Purchase Confirmed()
    {
        var p = new PurchaseBuilder().Build();
        p.ApplyDecision(Outcome.Approved);
        return p;
    }

    public static Purchase Cancelled()
    {
        var p = new PurchaseBuilder().Build();
        p.ApplyDecision(Outcome.Rejected);
        return p;
    }

    public static Purchase UnderReview()
    {
        var p = new PurchaseBuilder().Build();
        p.ApplyDecision(Outcome.Review);
        return p;
    }
}

public sealed class PurchaseBuilder
{
    private Guid _id = TestConstants.Ids.PurchaseId;
    private Guid _bookId = TestConstants.Ids.BookId;
    private int _quantity = 1;
    private decimal _total = 29.99m;
    private string _currency = "BRL";
    private string _correlationId = "corr-0001";

    public PurchaseBuilder WithId(Guid id) { _id = id; return this; }
    public PurchaseBuilder WithQuantity(int quantity) { _quantity = quantity; return this; }
    public PurchaseBuilder WithTotal(decimal total) { _total = total; return this; }
    public PurchaseBuilder WithCorrelationId(string id) { _correlationId = id; return this; }

    // Status always starts as PendingFraudCheck (domain default); use Mother methods for other states
    public Purchase Build() => new()
    {
        Id = _id,
        BookId = _bookId,
        Quantity = _quantity,
        Total = new Money(_total, _currency),
        CorrelationId = _correlationId,
        CreatedAt = TestConstants.Dates.FixedUtcNow
    };
}
