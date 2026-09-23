namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed record ApplyFraudDecisionResponse(Guid PurchaseId, string Status);
