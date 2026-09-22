using Domain.Entities.FraudAnalysis;

namespace Domain.Rules.Transactional;

public sealed class BulkQuantityRule : IFraudRule
{
    public string Code => "BULK_QUANTITY";
    public string Version => "1.0";

    /// <summary>Item count at or above which the rule triggers. Bulk purchases of the same item warrant review (S3).</summary>
    public const int ItemCountThreshold = 5;

    /// <summary>Risk weight applied when the rule fires. Alone reaches ReviewThreshold, directing bulk orders to human review.</summary>
    public const decimal Weight = 0.4m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        var hit = context.ItemCount >= ItemCountThreshold;
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? $"BULK_QUANTITY: {context.ItemCount} items ordered."
                : "BULK_QUANTITY: not triggered.");
    }
}
