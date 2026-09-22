namespace Application.Messages;

/// <summary>Published when a purchase is confirmed by the BookStore API; triggers fraud evaluation.</summary>
public sealed record PurchasePlaced(
    Guid PurchaseId,
    Guid BookId,
    int Quantity,
    decimal TotalAmount,
    string Currency,
    string BookFormat,
    string PaymentType,
    string PaymentFingerprint,
    string? PaymentLast4,
    string CustomerId,
    string CorrelationId,
    DateTime PlacedAt);
