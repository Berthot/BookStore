using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Diagnostics;
using Application.Messages;
using Cortex.Mediator.Commands;
using Domain.Enums;
using Domain.Repositories;
using Domain.Rules;
using Domain.Rules.Transactional;

namespace Application.UseCases.FraudAnalysis.AssessTransaction;

public sealed class AssessTransactionHandler(
    ITransactionRepository repository,
    IFraudUnitOfWork unitOfWork,
    IEventPublisher publisher,
    FraudRuleSet ruleSet)
    : ICommandHandler<AssessTransactionRequest, OperationResult<AssessTransactionResponse>>
{
    /// <summary>Window used for card velocity and structuring signal queries.</summary>
    private const int SignalWindowDays = 30;

    /// <summary>Lower bound (inclusive) for structuring detection: 90% of HighValueDigital threshold.</summary>
    private const decimal StructuringLowerBound = HighValueDigitalRule.AmountThreshold * 0.9m;

    /// <summary>Upper bound (exclusive) for structuring detection: the HighValueDigital threshold itself.</summary>
    private const decimal StructuringUpperBound = HighValueDigitalRule.AmountThreshold;

    public async Task<OperationResult<AssessTransactionResponse>> Handle(
        AssessTransactionRequest command,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
            return OperationResult<AssessTransactionResponse>.Fail(ErrorCode.NotFound, "Transaction not found.");

        // Idempotency: already decided — nothing to do
        if (transaction.Status == TransactionStatus.Decided)
            return OperationResult<AssessTransactionResponse>.SuccessResult(
                new AssessTransactionResponse(
                    transaction.Id,
                    transaction.CurrentAssessment()!.Outcome));

        // First commit: advance to Processing so GET shows PROCESSING
        if (transaction.Status == TransactionStatus.Received)
        {
            var processingError = transaction.StartProcessing();
            if (processingError is not null)
                return OperationResult<AssessTransactionResponse>.Fail(ErrorCode.Unprocessable, processingError.Message);

            await unitOfWork.CommitAsync(cancellationToken);

            // Demo delay: transaction is now visibly PROCESSING; sleep keeps it there so the dashboard shows the intermediate status
            if (command.DelaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(command.DelaySeconds), cancellationToken);
        }

        // Compute signals — all queries use the (payment_fingerprint, occurred_at) index
        var since = transaction.OccurredAt.AddDays(-SignalWindowDays);

        var recentWithSameCard = await repository.CountRecentByFingerprintAsync(
            transaction.PaymentFingerprint, since, transaction.Id, cancellationToken);

        var (customerCount, customerAvg) = await repository.GetCustomerStatsAsync(
            transaction.CustomerId, transaction.Id, cancellationToken);

        var justBelowCount = await repository.CountJustBelowThresholdAsync(
            transaction.CustomerId, StructuringLowerBound, StructuringUpperBound,
            since, transaction.Id, cancellationToken);

        var context = new FraudContext(
            transaction.Amount,
            transaction.DeliveryType,
            transaction.ItemCount,
            transaction.Channel,
            transaction.OccurredAt,
            IsNewCustomer: customerCount == 0,
            RecentTransactionsWithSameCard: recentWithSameCard,
            CustomerAverageAmount: customerAvg,
            CustomerTransactionCount: customerCount,
            RecentJustBelowThresholdCount: justBelowCount);

        var now = DateTime.UtcNow;

        // Sibling span 1: rule evaluation — disposed before the decide span opens
        Domain.Entities.FraudAnalysis.Assessment assessment;
        {
            using var evalSpan = FraudTelemetry.ActivitySource.StartActivity("fraud.rules.evaluate");
            evalSpan?.SetTag("transaction.id", transaction.Id);

            assessment = ruleSet.Evaluate(transaction.Id, context, now);

            var score = (int)Math.Round(assessment.Evaluations.Sum(e => e.Weight) * 100);

            foreach (var eval in assessment.Evaluations.Where(e => e.Hit))
                FraudTelemetry.RuleHits.Add(1,
                    new System.Collections.Generic.KeyValuePair<string, object?>("rule_code", eval.RuleCode));

            evalSpan?.SetTag("fraud.rules.hit_count", assessment.Evaluations.Count(e => e.Hit));
            evalSpan?.SetTag("fraud.outcome", assessment.Outcome.ToString());
            evalSpan?.SetTag("fraud.score", score);
        }

        // Sibling span 2: persist decision and publish event — starts after evalSpan is disposed
        {
            using var decideSpan = FraudTelemetry.ActivitySource.StartActivity("fraud.transaction.decide");
            decideSpan?.SetTag("transaction.id", transaction.Id);
            decideSpan?.SetTag("fraud.outcome", assessment.Outcome.ToString());
            decideSpan?.SetTag("fraud.score",
                (int)Math.Round(assessment.Evaluations.Sum(e => e.Weight) * 100));

            var decideError = transaction.Decide(assessment);
            if (decideError is not null)
                return OperationResult<AssessTransactionResponse>.Fail(ErrorCode.Unprocessable, decideError.Message);

            repository.AddAssessment(assessment);

            await unitOfWork.CommitAsync(cancellationToken);

            await publisher.PublishAsync(
                new TransactionDecided(
                    transaction.Id,
                    assessment.Outcome,
                    transaction.CorrelationId,
                    now),
                cancellationToken);
        }

        FraudTelemetry.Decisions.Add(1,
            new System.Collections.Generic.KeyValuePair<string, object?>("outcome", assessment.Outcome));
        FraudTelemetry.DecisionDuration.Record(
            (now - transaction.CreatedAt).TotalSeconds,
            new System.Collections.Generic.KeyValuePair<string, object?>("outcome", assessment.Outcome));

        return OperationResult<AssessTransactionResponse>.SuccessResult(
            new AssessTransactionResponse(transaction.Id, assessment.Outcome));
    }
}
