using Application.Commons;
using Application.Diagnostics;
using Cortex.Mediator.Commands;
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
                new ApplyFraudDecisionResponse(purchase.Id, purchase.Status.ToString().ToUpperInvariant()));

        if (!Enum.TryParse<Outcome>(command.Outcome, ignoreCase: true, out var outcome))
            return OperationResult<ApplyFraudDecisionResponse>.Fail(ErrorCode.Unprocessable, $"Unknown outcome: {command.Outcome}");

        var error = purchase.ApplyDecision(outcome);
        if (error is not null)
            return OperationResult<ApplyFraudDecisionResponse>.Fail(ErrorCode.Unprocessable, error.Message);

        await unitOfWork.CommitAsync(cancellationToken);

        BookStoreTelemetry.Purchases.Add(1);

        return OperationResult<ApplyFraudDecisionResponse>.SuccessResult(
            new ApplyFraudDecisionResponse(purchase.Id, purchase.Status.ToString().ToUpperInvariant()));
    }
}
