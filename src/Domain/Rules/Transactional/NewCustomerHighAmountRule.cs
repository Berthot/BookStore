using Domain.Entities.FraudAnalysis;

namespace Domain.Rules.Transactional;

public sealed class NewCustomerHighAmountRule : IFraudRule
{
    public string Code => "NEW_CUSTOMER_HIGH_AMOUNT";
    public string Version => "1.0";

    /// <summary>Amount above which a new customer triggers this rule. Combined with HIGH_VALUE_DIGITAL covers S2.</summary>
    public const decimal AmountThreshold = 300m;

    /// <summary>Risk weight applied when the rule fires. Combined with HIGH_VALUE_DIGITAL exceeds RejectThreshold (S2).</summary>
    public const decimal Weight = 0.4m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        var hit = context.IsNewCustomer && context.Amount.Value > AmountThreshold;
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? $"NEW_CUSTOMER_HIGH_AMOUNT: new customer with amount {context.Amount.Value}."
                : "NEW_CUSTOMER_HIGH_AMOUNT: not triggered.");
    }
}
