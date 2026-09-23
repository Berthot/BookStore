using Application.Messages;
using Application.UseCases.FraudAnalysis.FailSafe;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedFaultConsumer(IMediator mediator) : IConsumer<Fault<TransactionSubmitted>>
{
    public async Task Consume(ConsumeContext<Fault<TransactionSubmitted>> context)
    {
        var command = new FailSafeTransactionRequest(
            context.Message.Message.TransactionId,
            "Processing unavailable after maximum retry attempts.");

        await mediator.SendCommandAsync(command, context.CancellationToken);
    }
}
