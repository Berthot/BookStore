using Application.Commons;
using Cortex.Mediator.Commands;
using Domain.Entities.Sales;
using Domain.Enums;

namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed record ApplyFraudDecisionRequest(
    Guid TransactionId,
    Outcome Outcome,
    int Score = 0,
    string DecidedBy = "ENGINE",
    IReadOnlyList<FraudTriggeredRule>? TriggeredRules = null) : ICommand<OperationResult<ApplyFraudDecisionResponse>>;
