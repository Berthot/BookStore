using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.FailSafe;
using Cortex.Mediator;
using MassTransit;

namespace Fraud.Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedFaultConsumer(
    IMediator mediator,
    ILogger<TransactionSubmittedFaultConsumer> logger) : IConsumer<Fault<TransactionSubmitted>>
{
    public async Task Consume(ConsumeContext<Fault<TransactionSubmitted>> context)
    {
        logger.LogWarning("Consuming {MessageType} fault for transaction {TransactionId} — applying fail-safe",
            nameof(TransactionSubmitted), context.Message.Message.TransactionId);

        var result = await mediator.SendCommandAsync(
            new FailSafeTransactionRequest(
                context.Message.Message.TransactionId,
                "Processing unavailable after maximum retry attempts."),
            context.CancellationToken);

        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"FailSafeTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        logger.LogInformation("Fail-safe applied for transaction {TransactionId}: {Outcome}",
            context.Message.Message.TransactionId,
            result.IsSuccess ? result.Data?.Outcome.ToString() : "skipped (not found)");
    }
}
