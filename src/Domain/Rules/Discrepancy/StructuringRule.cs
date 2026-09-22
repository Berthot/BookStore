using Domain.Entities.FraudAnalysis;

namespace Domain.Rules.Discrepancy;

public sealed class StructuringRule : IFraudRule
{
    public string Code => "STRUCTURING";
    public string Version => "1.0";

    /// <summary>Number of recent transactions just below a threshold required to flag structuring (S11). Three occurrences suggest deliberate avoidance of a reporting limit.</summary>
    public const int RecentJustBelowThresholdCount = 3;

    /// <summary>Risk weight applied when the rule fires. Alone reaches ReviewThreshold — structuring warrants investigation, not automatic rejection (ADR-0008).</summary>
    public const decimal Weight = 0.4m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        var hit = context.RecentJustBelowThresholdCount >= RecentJustBelowThresholdCount;
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? $"STRUCTURING: {context.RecentJustBelowThresholdCount} recent transactions just below threshold."
                : "STRUCTURING: not triggered.");
    }
}
