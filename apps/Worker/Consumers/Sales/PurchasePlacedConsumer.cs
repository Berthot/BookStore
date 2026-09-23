using Application.Messages;
using Application.UseCases.Sales.SubmitPurchaseToFraud;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.Sales;

public sealed class PurchasePlacedConsumer(IMediator mediator) : IConsumer<PurchasePlaced>
{
    public async Task Consume(ConsumeContext<PurchasePlaced> context)
    {
        await mediator.SendCommandAsync(
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
    }
}
