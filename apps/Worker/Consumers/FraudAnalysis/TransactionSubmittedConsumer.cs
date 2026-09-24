using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.AssessTransaction;
using Cortex.Mediator;
using MassTransit;

namespace Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedConsumer(
    IMediator mediator,
    ILogger<TransactionSubmittedConsumer> logger) : IConsumer<TransactionSubmitted>
{
    public async Task Consume(ConsumeContext<TransactionSubmitted> context)
    {
        logger.LogInformation("Consuming {MessageType} for transaction {TransactionId}",
            nameof(TransactionSubmitted), context.Message.TransactionId);

        var result = await mediator.SendCommandAsync(
            new AssessTransactionRequest(context.Message.TransactionId),
            context.CancellationToken);

        // NotFound is idempotent: transaction was never persisted or already cleaned up.
        // Any other failure throws so MassTransit retries; after exhaustion, Fault<TransactionSubmitted>
        // triggers TransactionSubmittedFaultConsumer → FailSafe → REVIEW.
        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"AssessTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        logger.LogInformation("Processed {MessageType} for transaction {TransactionId}: outcome {Outcome}",
            nameof(TransactionSubmitted), context.Message.TransactionId,
            result.IsSuccess ? result.Data?.Outcome.ToString() : "skipped (not found)");
    }
}
