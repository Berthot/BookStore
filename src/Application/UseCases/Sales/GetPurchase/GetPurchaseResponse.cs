namespace Application.UseCases.Sales.GetPurchase;

public sealed record GetPurchaseResponse(
    Guid PurchaseId,
    string Status,
    string CustomerMessage,
    string? FraudDetails,
    DateTime CreatedAt);
