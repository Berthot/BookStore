using Domain.Bases;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.FraudAnalysis;

public sealed class Transaction : Entity
{
    private readonly List<Assessment> _assessments = [];

    public string ExternalReference { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public Money Amount { get; init; } = new(0, "BRL");
    public Channel Channel { get; init; }
    public DeliveryType DeliveryType { get; init; }
    public PaymentInstrument Payment { get; init; } = new("Unknown", string.Empty);
    // Stored as a first-class column so it can be part of a composite index (EF Core cannot index complex-type sub-properties directly)
    public string PaymentFingerprint { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public DateTime OccurredAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public TransactionStatus Status { get; private set; }
    public IReadOnlyList<Assessment> Assessments => _assessments;

    /// <summary>Returns the most recent assessment, or null if none exist.</summary>
    public Assessment? CurrentAssessment() => _assessments.MaxBy(a => a.CreatedAt);

    /// <summary>Creates a new transaction; returns a domain error if Amount.Value is not positive (I-01).</summary>
    public static (Transaction?, DomainError?) Create(
        string externalReference,
        string customerId,
        Money amount,
        Channel channel,
        DeliveryType deliveryType,
        PaymentInstrument payment,
        int itemCount,
        DateTime occurredAt,
        string correlationId,
        DateTime now)
    {
        if (amount.Value <= 0)
            return (null, new DomainError("TRANSACTION_AMOUNT_INVALID", "Amount must be greater than zero."));

        return (new Transaction
        {
            Id = Guid.NewGuid(),
            ExternalReference = externalReference,
            CustomerId = customerId,
            Amount = amount,
            Channel = channel,
            DeliveryType = deliveryType,
            Payment = payment,
            PaymentFingerprint = payment.Fingerprint,
            ItemCount = itemCount,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            Status = TransactionStatus.Received,
            CreatedAt = now
        }, null);
    }

    // I-02: valid transition is Received → Processing only; Decided is a terminal status.
    public DomainError? StartProcessing()
    {
        if (Status == TransactionStatus.Processing)
            return new DomainError("TRANSACTION_ALREADY_PROCESSING", "Transaction is already being processed.");
        if (Status == TransactionStatus.Decided)
            return new DomainError("TRANSACTION_ALREADY_DECIDED", "A decided transaction cannot be reprocessed.");

        Status = TransactionStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    /// <summary>Records an engine assessment and advances to Decided (I-03). Requires Processing status.</summary>
    public DomainError? Decide(Assessment assessment)
    {
        if (Status != TransactionStatus.Processing)
            return new DomainError("TRANSACTION_NOT_PROCESSING", "Engine assessment requires Processing status.");

        if (assessment.Decider.Kind != DeciderKind.Engine)
            return new DomainError("TRANSACTION_INVALID_DECIDER", "Decide() requires an engine assessment.");

        _assessments.Add(assessment);
        Status = TransactionStatus.Decided;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    /// <summary>Appends a reviewer assessment (I-04, I-05). Requires Decided status, current outcome Review, justification, and non-Review outcome.</summary>
    public DomainError? Review(Assessment assessment)
    {
        if (Status != TransactionStatus.Decided)
            return new DomainError("TRANSACTION_NOT_DECIDED", "Reviewer assessment requires Decided status.");

        var current = CurrentAssessment();
        if (current?.Outcome != Outcome.Review)
            return new DomainError("TRANSACTION_REVIEW_NOT_PENDING", "Reviewer assessment requires current outcome to be Review.");

        if (assessment.Decider.Kind != DeciderKind.Reviewer)
            return new DomainError("TRANSACTION_INVALID_DECIDER", "Review() requires a reviewer assessment.");

        if (string.IsNullOrWhiteSpace(assessment.Justification))
            return new DomainError("TRANSACTION_JUSTIFICATION_REQUIRED", "Reviewer assessment requires a justification.");

        if (assessment.Outcome == Outcome.Review)
            return new DomainError("TRANSACTION_REVIEW_OUTCOME_INVALID", "Reviewer assessment cannot have Review outcome.");

        _assessments.Add(assessment);
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    /// <summary>Applies a system fail-safe assessment with Review outcome (I-07). Requires Processing status.</summary>
    public DomainError? FailSafe(string reason)
    {
        if (Status != TransactionStatus.Processing)
            return new DomainError("TRANSACTION_NOT_PROCESSING", "Fail-safe requires Processing status.");

        _assessments.Add(Assessment.ForSystem(Id, reason, DateTime.UtcNow));
        Status = TransactionStatus.Decided;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }
}
