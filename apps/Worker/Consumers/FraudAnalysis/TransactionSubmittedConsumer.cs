using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.AssessTransaction;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedConsumer(IMediator mediator) : IConsumer<TransactionSubmitted>
{
    public async Task Consume(ConsumeContext<TransactionSubmitted> context)
    {
        var result = await mediator.SendCommandAsync(
            new AssessTransactionRequest(context.Message.TransactionId),
            context.CancellationToken);

        // NotFound is idempotent: transaction was never persisted or already cleaned up.
        // Any other failure throws so MassTransit retries; after exhaustion, Fault<TransactionSubmitted>
        // triggers TransactionSubmittedFaultConsumer → FailSafe → REVIEW.
        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"AssessTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");
    }
}
