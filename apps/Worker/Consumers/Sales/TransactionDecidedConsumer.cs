using Application.Commons;
using Application.Messages;
using Application.UseCases.Sales.ApplyFraudDecision;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.Sales;

public sealed class TransactionDecidedConsumer(IMediator mediator) : IConsumer<TransactionDecided>
{
    public async Task Consume(ConsumeContext<TransactionDecided> context)
    {
        var result = await mediator.SendCommandAsync(
            new ApplyFraudDecisionRequest(context.Message.TransactionId, context.Message.Outcome),
            context.CancellationToken);

        // NotFound is idempotent: the purchase was never linked or already cleaned up.
        // Any other failure throws so MassTransit retries and ultimately routes to the error queue.
        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"ApplyFraudDecision failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");
    }
}
