using Application.Commons;
using Cortex.Mediator.Queries;
using Domain.Entities.FraudAnalysis;
using Domain.Repositories;

namespace Application.UseCases.FraudAnalysis.GetTransaction;

public sealed class GetTransactionHandler(ITransactionRepository repository)
    : IQueryHandler<GetTransactionRequest, OperationResult<GetTransactionResponse>>
{
    public async Task<OperationResult<GetTransactionResponse>> Handle(
        GetTransactionRequest query,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(query.TransactionId, cancellationToken);

        if (transaction is null)
            return OperationResult<GetTransactionResponse>.Fail(ErrorCode.NotFound, "Transaction not found.");

        return OperationResult<GetTransactionResponse>.SuccessResult(MapToResponse(transaction));
    }

    internal static GetTransactionResponse MapToResponse(Transaction t)
    {
        var current = t.CurrentAssessment();
        var sorted = t.Assessments.OrderBy(a => a.CreatedAt).ToList();

        return new GetTransactionResponse(
            t.Id,
            t.Status.ToString().ToUpperInvariant(),
            new MoneyDto(t.Amount.Value, t.Amount.Currency),
            t.CreatedAt,
            current is null ? null : MapDecision(current),
            sorted.Select(MapHistory).ToList());
    }

    private static DecisionDto MapDecision(Assessment a) =>
        new(
            a.Outcome.ToString().ToUpperInvariant(),
            (int)Math.Round(a.Evaluations.Sum(e => e.Weight) * 100),
            new DeciderDto(a.Decider.Kind.ToString().ToUpperInvariant(), a.Decider.ReviewerId),
            a.CreatedAt,
            a.Justification,
            a.Evaluations.Select(MapRule).ToList());

    private static RuleDto MapRule(RuleEvaluation e) =>
        new(
            e.RuleCode,
            int.TryParse(e.RuleVersion.Split('.')[0], out var v) ? v : 1,
            e.Hit,
            (int)Math.Round(e.Weight * 100),
            e.Reason);

    private static HistoryEntryDto MapHistory(Assessment a) =>
        new(
            a.Outcome.ToString().ToUpperInvariant(),
            new DeciderDto(a.Decider.Kind.ToString().ToUpperInvariant(), a.Decider.ReviewerId),
            a.CreatedAt);
}
