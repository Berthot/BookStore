using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.AssessTransaction;
using Cortex.Mediator;
using Infrastructure.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Fraud.Worker.Consumers.FraudAnalysis;

public sealed class TransactionSubmittedConsumer(
    IMediator mediator,
    IOptions<DemoOptions> demoOptions,
    ILogger<TransactionSubmittedConsumer> logger) : IConsumer<TransactionSubmitted>
{
    public async Task Consume(ConsumeContext<TransactionSubmitted> context)
    {
        logger.LogInformation("Consuming {MessageType} for transaction {TransactionId}",
            nameof(TransactionSubmitted), context.Message.TransactionId);

        var delay = demoOptions.Value.FraudProcessingDelaySeconds;

        var result = await mediator.SendCommandAsync(
            new AssessTransactionRequest(context.Message.TransactionId, delay),
            context.CancellationToken);

        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"AssessTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        logger.LogInformation("Processed {MessageType} for transaction {TransactionId}: outcome {Outcome}",
            nameof(TransactionSubmitted), context.Message.TransactionId,
            result.IsSuccess ? result.Data?.Outcome.ToString() : "skipped (not found)");
    }
}
