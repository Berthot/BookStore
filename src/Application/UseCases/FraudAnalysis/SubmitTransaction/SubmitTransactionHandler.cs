using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Diagnostics;
using Application.Messages;
using Cortex.Mediator.Commands;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;

namespace Application.UseCases.FraudAnalysis.SubmitTransaction;

public sealed class SubmitTransactionHandler(
    ITransactionRepository repository,
    IFraudUnitOfWork unitOfWork,
    IEventPublisher publisher)
    : ICommandHandler<SubmitTransactionRequest, OperationResult<SubmitTransactionResponse>>
{
    public async Task<OperationResult<SubmitTransactionResponse>> Handle(
        SubmitTransactionRequest command,
        CancellationToken cancellationToken)
    {
        Enum.TryParse<Channel>(command.Channel, ignoreCase: true, out var channel);
        Enum.TryParse<DeliveryType>(command.Delivery, ignoreCase: true, out var delivery);

        var amount = new Money(command.AmountValue, command.AmountCurrency);
        var payment = new PaymentInstrument(command.PaymentType, command.PaymentFingerprint, command.PaymentLast4);

        var (transaction, error) = Domain.Entities.FraudAnalysis.Transaction.Create(
            command.ExternalReference ?? string.Empty,
            command.CustomerId,
            amount,
            channel,
            delivery,
            payment,
            command.ItemCount,
            command.OccurredAt,
            command.CorrelationId,
            DateTime.UtcNow);

        if (error is not null)
            return OperationResult<SubmitTransactionResponse>.Fail(ErrorCode.Validation, error.Message);

        repository.Add(transaction!);

        await publisher.PublishAsync(new TransactionSubmitted(
            transaction!.Id,
            transaction.ExternalReference,
            transaction.CustomerId,
            transaction.Amount.Value,
            transaction.Amount.Currency,
            transaction.Channel.ToString(),
            transaction.DeliveryType.ToString(),
            transaction.Payment.Type,
            transaction.Payment.Fingerprint,
            transaction.Payment.Last4,
            transaction.ItemCount,
            transaction.OccurredAt,
            transaction.CorrelationId,
            transaction.CreatedAt), cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        FraudTelemetry.TransactionsReceived.Add(1);

        return OperationResult<SubmitTransactionResponse>.SuccessResult(
            new SubmitTransactionResponse(transaction.Id, transaction.Status.ToString().ToUpperInvariant()));
    }
}
