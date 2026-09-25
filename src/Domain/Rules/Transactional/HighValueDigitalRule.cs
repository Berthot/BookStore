using System.Globalization;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;

namespace Domain.Rules.Transactional;

public sealed class HighValueDigitalRule : IFraudRule
{
    public string Code => "HIGH_VALUE_DIGITAL";
    public string Version => "1.0";

    /// <summary>Digital purchases above this amount trigger the rule. Set to cover premium e-book scenarios (S2).</summary>
    public const decimal AmountThreshold = 500m;

    /// <summary>Risk weight applied when the rule fires. Combined with NEW_CUSTOMER_HIGH_AMOUNT reaches RejectThreshold.</summary>
    public const decimal Weight = 0.4m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        var hit = context.Delivery == DeliveryType.Digital && context.Amount.Value > AmountThreshold;
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? FormattableString.Invariant($"HIGH_VALUE_DIGITAL: digital delivery with amount {context.Amount.Value}.")
                : "HIGH_VALUE_DIGITAL: not triggered.");
    }
}
