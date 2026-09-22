using Domain.Bases;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.Sales;

public sealed class Purchase : Entity
{
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
    public Money Total { get; init; } = new(0, "BRL");
    public Guid? TransactionId { get; private set; }
    public string CorrelationId { get; init; } = string.Empty;
    public PurchaseStatus Status { get; private set; }

    public static Purchase Create(Guid bookId, int quantity, Money total, string correlationId, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Quantity = quantity,
            Total = total,
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
