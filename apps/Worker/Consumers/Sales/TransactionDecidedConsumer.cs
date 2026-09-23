using Application.Commons;
using Application.Messages;
using Application.UseCases.Sales.ApplyFraudDecision;
using Cortex.Mediator;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers.Sales;

public sealed class TransactionDecidedConsumer(
    IMediator mediator,
    ILogger<TransactionDecidedConsumer> logger) : IConsumer<TransactionDecided>
{
    public async Task Consume(ConsumeContext<TransactionDecided> context)
    {
        logger.LogInformation(
            "Consuming {MessageType} for transaction {TransactionId} with outcome {Outcome}",
            nameof(TransactionDecided), context.Message.TransactionId, context.Message.Outcome);

        var result = await mediator.SendCommandAsync(
            new ApplyFraudDecisionRequest(context.Message.TransactionId, context.Message.Outcome),
            context.CancellationToken);

        // NotFound is idempotent: the purchase was never linked or already cleaned up.
        // Any other failure throws so MassTransit retries and ultimately routes to the error queue.
        if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            throw new InvalidOperationException(
                $"ApplyFraudDecision failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        logger.LogInformation(
            "Applied fraud decision for transaction {TransactionId}: purchase status {PurchaseStatus}",
            context.Message.TransactionId,
            result.IsSuccess ? result.Data?.Status.ToString() : "skipped (not found)");
    }
}
