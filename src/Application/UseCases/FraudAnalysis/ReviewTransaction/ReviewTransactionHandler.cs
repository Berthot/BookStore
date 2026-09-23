using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Cortex.Mediator.Commands;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases.FraudAnalysis.ReviewTransaction;

public sealed class ReviewTransactionHandler(
    ITransactionRepository repository,
    IFraudUnitOfWork unitOfWork,
    IEventPublisher publisher)
    : ICommandHandler<ReviewTransactionRequest, OperationResult<GetTransactionResponse>>
{
    /// <summary>Generic reviewer ID for all review assessments. In production this would come from the auth token; for this tech test a single reviewer is assumed (D-49).</summary>
    public const string DefaultReviewerId = "reviewer-default";

    public async Task<OperationResult<GetTransactionResponse>> Handle(
        ReviewTransactionRequest command,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.NotFound, "Transaction not found.");

        // 409 when current outcome is not Review (includes second review — idempotency without Idempotency-Key)
        var current = transaction.CurrentAssessment();
        if (current is null || current.Outcome != Outcome.Review)
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.Conflict,
                "Transaction is not pending review.");

        if (!Enum.TryParse<Outcome>(command.Outcome, ignoreCase: true, out var outcome))
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.Unprocessable,
                $"Invalid outcome '{command.Outcome}'. Use APPROVED or REJECTED.");

        if (outcome == Outcome.Review)
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.Unprocessable,
                "Reviewer outcome cannot be Review.");

        if (string.IsNullOrWhiteSpace(command.Justification))
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.Unprocessable,
                "Justification is required.");

        var now = DateTime.UtcNow;
        var assessment = Assessment.ForReviewer(
            transaction.Id,
            outcome,
            command.Justification,
            DefaultReviewerId,
            now);

        var reviewError = transaction.Review(assessment);
        if (reviewError is not null)
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.Unprocessable, reviewError.Message);

        repository.AddAssessment(assessment);

        await unitOfWork.CommitAsync(cancellationToken);

        await publisher.PublishAsync(
            new TransactionDecided(
                transaction.Id,
                outcome.ToString().ToUpperInvariant(),
                transaction.CorrelationId,
                now),
            cancellationToken);

        return OperationResult<GetTransactionResponse>.SuccessResult(
            GetTransactionHandler.MapToResponse(transaction));
    }
}
