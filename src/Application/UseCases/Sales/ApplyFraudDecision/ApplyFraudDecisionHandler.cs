using Application.Commons;
using Application.Diagnostics;
using Cortex.Mediator.Commands;
using Domain.Entities.Sales;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases.Sales.ApplyFraudDecision;

public sealed class ApplyFraudDecisionHandler(
    IPurchaseRepository purchaseRepository,
    IBookStoreUnitOfWork unitOfWork)
    : ICommandHandler<ApplyFraudDecisionRequest, OperationResult<ApplyFraudDecisionResponse>>
{
    public async Task<OperationResult<ApplyFraudDecisionResponse>> Handle(
        ApplyFraudDecisionRequest command,
        CancellationToken cancellationToken)
    {
        var purchase = await purchaseRepository.GetByTransactionIdAsync(command.TransactionId, cancellationToken);
        if (purchase is null)
            return OperationResult<ApplyFraudDecisionResponse>.Fail(ErrorCode.NotFound, "Purchase not found for the given transaction.");

        if (purchase.Status is PurchaseStatus.Confirmed or PurchaseStatus.Cancelled)
            return OperationResult<ApplyFraudDecisionResponse>.SuccessResult(
                new ApplyFraudDecisionResponse(purchase.Id, purchase.Status));

        var fraudOutcome = command.Outcome == Outcome.Review
            ? null
            : new FraudOutcomeSnapshot(
                command.Score,
                command.DecidedBy,
                command.TriggeredRules ?? []);

        var error = purchase.ApplyDecision(command.Outcome, fraudOutcome);
        if (error is not null)
            return OperationResult<ApplyFraudDecisionResponse>.Fail(ErrorCode.Unprocessable, error.Message);

        await unitOfWork.CommitAsync(cancellationToken);

        BookStoreTelemetry.Purchases.Add(1,
            new System.Collections.Generic.KeyValuePair<string, object?>("status", purchase.Status.ToString()));

        return OperationResult<ApplyFraudDecisionResponse>.SuccessResult(
            new ApplyFraudDecisionResponse(purchase.Id, purchase.Status));
    }
}
