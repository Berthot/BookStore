using Domain.Entities.FraudAnalysis;

namespace Domain.Rules.Discrepancy;

public sealed class AmountDeviationRule : IFraudRule
{
    public string Code => "AMOUNT_DEVIATION";
    public string Version => "1.0";

    /// <summary>Minimum number of past transactions required before the rule applies. Prevents false positives for new customers (covered by NEW_CUSTOMER_HIGH_AMOUNT).</summary>
    public const int MinHistoryCount = 3;

    /// <summary>Amount must exceed this multiple of the customer's historical average to trigger the rule (S10).</summary>
    public const decimal DeviationFactor = 3.0m;

    /// <summary>Risk weight applied when the rule fires. Alone reaches ReviewThreshold — discrepancy is a signal, not proof (ADR-0008).</summary>
    public const decimal Weight = 0.4m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        if (context.CustomerTransactionCount < MinHistoryCount)
            return new RuleEvaluation(Code, Version, false, 0m, "AMOUNT_DEVIATION: insufficient history.");

        var hit = context.CustomerAverageAmount > 0
            && context.Amount.Value > context.CustomerAverageAmount * DeviationFactor;

        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? $"AMOUNT_DEVIATION: {context.Amount.Value} exceeds {DeviationFactor}x average ({context.CustomerAverageAmount})."
                : "AMOUNT_DEVIATION: not triggered.");
    }
}
