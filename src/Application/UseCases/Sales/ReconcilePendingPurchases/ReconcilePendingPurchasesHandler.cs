using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Diagnostics;
using Application.Messages;
using Cortex.Mediator.Commands;
using Domain.Repositories;

namespace Application.UseCases.Sales.ReconcilePendingPurchases;

public sealed class ReconcilePendingPurchasesHandler(
    IPurchaseRepository purchaseRepository,
    IBookStoreUnitOfWork unitOfWork,
    IEventPublisher publisher)
    : ICommandHandler<ReconcilePendingPurchasesRequest, OperationResult<ReconcilePendingPurchasesResponse>>
{
    public async Task<OperationResult<ReconcilePendingPurchasesResponse>> Handle(
        ReconcilePendingPurchasesRequest command,
        CancellationToken cancellationToken)
    {
        var stalePurchases = await purchaseRepository.ListPendingFraudCheckAsync(command.Threshold, cancellationToken);

        foreach (var purchase in stalePurchases)
        {
            await publisher.PublishAsync(new PurchasePlaced(
                purchase.Id,
                purchase.BookId,
                purchase.Quantity,
                purchase.Total.Value,
                purchase.Total.Currency,
                purchase.BookFormat,
                purchase.PaymentType,
                purchase.PaymentFingerprint,
                purchase.PaymentLast4,
                purchase.CustomerId,
                purchase.CorrelationId,
                purchase.CreatedAt), cancellationToken);
        }

        // With the bus outbox, PublishAsync only stages the messages in the DbContext; they are persisted
        // (and later delivered) only when the unit of work commits.
        if (stalePurchases.Count > 0)
            await unitOfWork.CommitAsync(cancellationToken);

        BookStoreTelemetry.ReconciliationRepublished.Add(stalePurchases.Count);

        return OperationResult<ReconcilePendingPurchasesResponse>.SuccessResult(
            new ReconcilePendingPurchasesResponse(stalePurchases.Count));
    }
}
