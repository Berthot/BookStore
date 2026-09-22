using Domain.Entities.FraudAnalysis;

namespace Domain.Rules;

public interface IFraudRule
{
    /// <summary>Stable code identifying this rule (e.g. HIGH_VALUE_DIGITAL). Used in RuleEvaluation and audit records.</summary>
    string Code { get; }

    /// <summary>Semantic version of this rule. Increments whenever logic or thresholds change.</summary>
    string Version { get; }

    /// <summary>Evaluates the rule against the provided context and returns the evaluation result.</summary>
    RuleEvaluation Evaluate(FraudContext context);
}
