using Application.Commons;
using Cortex.Mediator.Commands;
using Domain.Enums;

namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed record ApplyFraudDecisionRequest(
    Guid TransactionId,
    Outcome Outcome) : ICommand<OperationResult<ApplyFraudDecisionResponse>>;
