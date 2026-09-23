using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.FailSafe;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedFaultConsumer(IMediator mediator) : IConsumer<Fault<TransactionSubmitted>>
{
    public async Task Consume(ConsumeContext<Fault<TransactionSubmitted>> context)
    {
        var result = await mediator.SendCommandAsync(
            new FailSafeTransactionRequest(
                context.Message.Message.TransactionId,
                "Processing unavailable after maximum retry attempts."),
            context.CancellationToken);

        // NotFound is idempotent: transaction was already cleaned up.
        // Any other failure throws so the fault is nacked and routed to the error queue.
        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"FailSafeTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");
    }
}
