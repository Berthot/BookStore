using Application.Messages;
using Application.UseCases.FraudAnalysis.AssessTransaction;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedConsumer(IMediator mediator) : IConsumer<TransactionSubmitted>
{
    public async Task Consume(ConsumeContext<TransactionSubmitted> context)
    {
        var command = new AssessTransactionRequest(context.Message.TransactionId);
        await mediator.SendCommandAsync(command, context.CancellationToken);
    }
}
