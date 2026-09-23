using Domain.Enums;

namespace Application.UseCases.Sales.GetPurchase;

public sealed record GetPurchaseResponse(
    Guid PurchaseId,
    PurchaseStatus Status,
    string CustomerMessage,
    string? FraudDetails,
    DateTime CreatedAt);
