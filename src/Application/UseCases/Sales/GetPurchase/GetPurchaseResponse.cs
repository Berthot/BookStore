using Domain.Enums;

namespace Application.UseCases.Sales.GetPurchase;

public sealed record FraudDetailsResponse(Guid? TransactionId, string? Outcome);

public sealed record GetPurchaseResponse(
    Guid PurchaseId,
    PurchaseStatus Status,
    string CustomerMessage,
    FraudDetailsResponse? FraudDetails,
    DateTime CreatedAt);
