namespace Application.UseCases.Sales.SubmitPurchaseToFraud;

public sealed record SubmitPurchaseToFraudResponse(Guid PurchaseId, Guid TransactionId);
