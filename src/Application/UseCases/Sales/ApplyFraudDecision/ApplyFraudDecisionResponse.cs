using Domain.Enums;

namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed record ApplyFraudDecisionResponse(Guid PurchaseId, PurchaseStatus Status);
