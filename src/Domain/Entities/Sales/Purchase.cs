using Domain.Bases;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.Sales;

public sealed class Purchase : Entity
{
    public Guid BookId { get; init; }
    public string BookFormat { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public Money Total { get; init; } = new(0, "BRL");
    public string CustomerId { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public string PaymentFingerprint { get; init; } = string.Empty;
    public string? PaymentLast4 { get; init; }
    public Guid? TransactionId { get; private set; }
    public string CorrelationId { get; init; } = string.Empty;
    public PurchaseStatus Status { get; private set; }

    public static Purchase Create(
        Guid bookId,
        string bookFormat,
        int quantity,
        Money total,
        string customerId,
        string paymentType,
        string paymentFingerprint,
        string? paymentLast4,
        string correlationId,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            BookFormat = bookFormat,
            Quantity = quantity,
            Total = total,
            CustomerId = customerId,
            PaymentType = paymentType,
            PaymentFingerprint = paymentFingerprint,
            PaymentLast4 = paymentLast4,
            CorrelationId = correlationId,
            Status = PurchaseStatus.PendingFraudCheck,
            CreatedAt = now
        };

    public DomainError? LinkTransaction(Guid transactionId)
    {
        if (TransactionId is not null)
            return new DomainError("PURCHASE_TRANSACTION_ALREADY_LINKED", "A transaction is already linked to this purchase.");

        TransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public DomainError? ApplyDecision(Outcome outcome)
    {
        if (Status is PurchaseStatus.Confirmed or PurchaseStatus.Cancelled)
            return new DomainError("PURCHASE_FINAL_STATE", $"Purchase in state {Status} is final and cannot be changed.");

        if (Status == PurchaseStatus.UnderReview && outcome == Outcome.Review)
            return new DomainError("PURCHASE_REVIEW_INVALID", "A purchase under review cannot be sent to review again.");

        Status = outcome switch
        {
            Outcome.Approved => PurchaseStatus.Confirmed,
            Outcome.Rejected => PurchaseStatus.Cancelled,
            Outcome.Review => PurchaseStatus.UnderReview,
            _ => Status
        };

        UpdatedAt = DateTime.UtcNow;
        return null;
    }
}
