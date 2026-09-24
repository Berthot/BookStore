using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Cortex.Mediator.Commands;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases.FraudAnalysis.FailSafe;

public sealed class FailSafeTransactionHandler(
    ITransactionRepository repository,
    IFraudUnitOfWork unitOfWork,
    IEventPublisher publisher)
    : ICommandHandler<FailSafeTransactionRequest, OperationResult<FailSafeTransactionResponse>>
{
    public async Task<OperationResult<FailSafeTransactionResponse>> Handle(
        FailSafeTransactionRequest command,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
            return OperationResult<FailSafeTransactionResponse>.Fail(ErrorCode.NotFound, "Transaction not found.");

        // Idempotency: already decided by a prior attempt
        if (transaction.Status == TransactionStatus.Decided)
            return OperationResult<FailSafeTransactionResponse>.SuccessResult(
                new FailSafeTransactionResponse(
                    transaction.Id,
                    transaction.CurrentAssessment()!.Outcome));

        // Ensure Processing status before calling FailSafe (transaction may still be Received if the
        // assess consumer crashed before its first commit)
        if (transaction.Status == TransactionStatus.Received)
            transaction.StartProcessing();

        var now = DateTime.UtcNow;
        var error = transaction.FailSafe(command.Reason);
        if (error is not null)
            return OperationResult<FailSafeTransactionResponse>.Fail(ErrorCode.Unprocessable, error.Message);

        repository.AddAssessment(transaction.CurrentAssessment()!);

        await unitOfWork.CommitAsync(cancellationToken);

        await publisher.PublishAsync(
            new TransactionDecided(
                transaction.Id,
                transaction.CurrentAssessment()!.Outcome,
                transaction.CorrelationId,
                now,
                Score: 0,
                DecidedBy: "SYSTEM",
                TriggeredRules: []),
            cancellationToken);

        return OperationResult<FailSafeTransactionResponse>.SuccessResult(
            new FailSafeTransactionResponse(
                transaction.Id,
                transaction.CurrentAssessment()!.Outcome));
    }
}
