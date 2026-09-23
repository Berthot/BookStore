using Application.Commons;
using Application.UseCases.Sales.Ports;
using Cortex.Mediator.Commands;
using Domain.Repositories;

namespace Application.UseCases.Sales.SubmitPurchaseToFraud;

public sealed class SubmitPurchaseToFraudHandler(
    IPurchaseRepository purchaseRepository,
    IBookStoreUnitOfWork unitOfWork,
    IFraudCheckGateway fraudGateway)
    : ICommandHandler<SubmitPurchaseToFraudRequest, OperationResult<SubmitPurchaseToFraudResponse>>
{
    public async Task<OperationResult<SubmitPurchaseToFraudResponse>> Handle(
        SubmitPurchaseToFraudRequest command,
        CancellationToken cancellationToken)
    {
        var purchase = await purchaseRepository.GetByIdAsync(command.PurchaseId, cancellationToken);
        if (purchase is null)
            return OperationResult<SubmitPurchaseToFraudResponse>.Fail(ErrorCode.NotFound, "Purchase not found.");

        if (purchase.TransactionId.HasValue)
            return OperationResult<SubmitPurchaseToFraudResponse>.SuccessResult(
                new SubmitPurchaseToFraudResponse(purchase.Id, purchase.TransactionId.Value));

        var transactionId = await fraudGateway.SubmitAsync(
            command.PurchaseId,
            command.BookFormat,
            command.Quantity,
            command.TotalAmount,
            command.Currency,
            command.PaymentType,
            command.PaymentFingerprint,
            command.PaymentLast4,
            command.CustomerId,
            command.CorrelationId,
            command.PlacedAt,
            cancellationToken);

        purchase.LinkTransaction(transactionId);
        await unitOfWork.CommitAsync(cancellationToken);

        return OperationResult<SubmitPurchaseToFraudResponse>.SuccessResult(
            new SubmitPurchaseToFraudResponse(purchase.Id, transactionId));
    }
}
