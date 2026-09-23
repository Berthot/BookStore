using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.ValueObjects;
using Tests.Shared.Constants;

namespace Tests.Shared.Mothers.FraudAnalysis;

public static class TransactionMother
{
    public static Transaction Received() => new TransactionBuilder().Build();

    public static Transaction Processing()
    {
        var t = new TransactionBuilder().Build();
        t.StartProcessing();
        return t;
    }

    public static Transaction DecidedApproved()
    {
        var t = Processing();
        t.Decide(AssessmentMother.EngineApproved(t.Id));
        return t;
    }

    public static Transaction DecidedRejected()
    {
        var t = Processing();
        t.Decide(AssessmentMother.EngineRejected(t.Id));
        return t;
    }

    public static Transaction DecidedReview()
    {
        var t = Processing();
        t.Decide(AssessmentMother.EngineReview(t.Id));
        return t;
    }
}

public sealed class TransactionBuilder
{
    private string _externalReference = "order-0001";
    private string _customerId = "cus_test_001";
    private Money _amount = new(100m, "BRL");
    private Channel _channel = Channel.Web;
    private DeliveryType _deliveryType = DeliveryType.Digital;
    private PaymentInstrument _payment = new("credit_card", "fp-0001", "4242");
    private int _itemCount = 1;
    private DateTime _occurredAt = TestConstants.Dates.FixedUtcNow;
    private string _correlationId = "corr-tx-0001";

    public TransactionBuilder WithAmount(decimal value, string currency = "BRL")
    {
        _amount = new Money(value, currency);
        return this;
    }

    public TransactionBuilder WithChannel(Channel channel) { _channel = channel; return this; }
    public TransactionBuilder WithDeliveryType(DeliveryType type) { _deliveryType = type; return this; }
    public TransactionBuilder WithCorrelationId(string id) { _correlationId = id; return this; }
    public TransactionBuilder WithCustomerId(string id) { _customerId = id; return this; }
    public TransactionBuilder WithItemCount(int count) { _itemCount = count; return this; }

    // Status defaults to Received (0); use Mother methods to reach other states
    public Transaction Build() => new()
    {
        Id = TestConstants.Ids.TransactionId,
        ExternalReference = _externalReference,
        CustomerId = _customerId,
        Amount = _amount,
        Channel = _channel,
        DeliveryType = _deliveryType,
        Payment = _payment,
        PaymentFingerprint = _payment.Fingerprint,
        ItemCount = _itemCount,
        OccurredAt = _occurredAt,
        CorrelationId = _correlationId,
        CreatedAt = TestConstants.Dates.FixedUtcNow
    };

    /// <summary>Calls Transaction.Create() so domain invariants (I-01) are enforced.</summary>
    public (Transaction?, Domain.Bases.DomainError?) BuildCreate() =>
        Transaction.Create(_externalReference, _customerId, _amount, _channel, _deliveryType, _payment,
            _itemCount, _occurredAt, _correlationId, TestConstants.Dates.FixedUtcNow);
}
