using Application.Commons;
using Application.Messages;
using Application.UseCases.Sales.SubmitPurchaseToFraud;
using Cortex.Mediator;
using MassTransit;

namespace BookStore.Api.Consumers.Sales;

public sealed class PurchasePlacedConsumer(
    IMediator mediator,
    ILogger<PurchasePlacedConsumer> logger) : IConsumer<PurchasePlaced>
{
    public async Task Consume(ConsumeContext<PurchasePlaced> context)
    {
        logger.LogInformation("Consuming {MessageType} for purchase {PurchaseId}",
            nameof(PurchasePlaced), context.Message.PurchaseId);

        var result = await mediator.SendCommandAsync(
            new SubmitPurchaseToFraudRequest(
                context.Message.PurchaseId,
                context.Message.BookId,
                context.Message.Quantity,
                context.Message.TotalAmount,
                context.Message.Currency,
                context.Message.BookFormat,
                context.Message.PaymentType,
                context.Message.PaymentFingerprint,
                context.Message.PaymentLast4,
                context.Message.CustomerId,
                context.Message.CorrelationId,
                context.Message.PlacedAt),
            context.CancellationToken);

        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"SubmitPurchaseToFraud failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        logger.LogInformation("Processed {MessageType} for purchase {PurchaseId}: {Outcome}",
            nameof(PurchasePlaced), context.Message.PurchaseId,
            result.IsSuccess ? "submitted" : "skipped (not found)");
    }
}
