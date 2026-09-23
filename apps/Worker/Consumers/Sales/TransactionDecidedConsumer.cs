using Application.Messages;
using Application.UseCases.Sales.ApplyFraudDecision;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.Sales;

public sealed class TransactionDecidedConsumer(IMediator mediator) : IConsumer<TransactionDecided>
{
    public async Task Consume(ConsumeContext<TransactionDecided> context)
    {
        await mediator.SendCommandAsync(
            new ApplyFraudDecisionRequest(context.Message.TransactionId, context.Message.Outcome),
            context.CancellationToken);
    }
}
