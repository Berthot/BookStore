using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed record ApplyFraudDecisionRequest(
    Guid TransactionId,
    string Outcome) : ICommand<OperationResult<ApplyFraudDecisionResponse>>;
