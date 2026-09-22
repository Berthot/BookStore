using Domain.Entities.FraudAnalysis;

namespace Domain.Rules;

public sealed class FraudRuleSet(IEnumerable<IFraudRule> rules)
{
    /// <summary>Runs every registered rule, sums hit weights, applies the decision policy, and returns an engine Assessment containing all evaluations.</summary>
    public Assessment Evaluate(Guid transactionId, FraudContext context, DateTime now)
    {
        var evaluations = rules.Select(r => r.Evaluate(context)).ToList();
        var score = evaluations.Where(e => e.Hit).Sum(e => e.Weight);
        var outcome = DecisionPolicy.Decide(score);
        return Assessment.ForEngine(transactionId, outcome, evaluations, now);
    }
}
