using Domain.Bases;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.FraudAnalysis;

public sealed class Assessment : Entity
{
    private readonly List<RuleEvaluation> _evaluations = [];

    public Guid TransactionId { get; init; }
    public Outcome Outcome { get; init; }
    public Decider Decider { get; init; } = new(DeciderKind.Engine);
    public string? Justification { get; init; }
    public IReadOnlyList<RuleEvaluation> Evaluations => _evaluations;

    public static Assessment ForEngine(
        Guid transactionId,
        Outcome outcome,
        IEnumerable<RuleEvaluation> evaluations,
        DateTime now)
    {
        var a = new Assessment
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            Outcome = outcome,
            Decider = new Decider(DeciderKind.Engine),
            CreatedAt = now
        };
        a._evaluations.AddRange(evaluations);
        return a;
    }

    public static Assessment ForReviewer(
        Guid transactionId,
        Outcome outcome,
        string justification,
        string reviewerId,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            Outcome = outcome,
            Decider = new Decider(DeciderKind.Reviewer, reviewerId),
            Justification = justification,
            CreatedAt = now
        };

    public static Assessment ForSystem(
        Guid transactionId,
        string reason,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            Outcome = Outcome.Review,
            Decider = new Decider(DeciderKind.System),
            Justification = reason,
            CreatedAt = now
        };
}
