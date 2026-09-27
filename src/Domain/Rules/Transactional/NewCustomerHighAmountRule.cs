using System.Globalization;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;

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
        // Only targets high-value digital purchases by new customers: Physical bulk purchases
        // are already covered by BulkQuantityRule and should not double-count here.
        var hit = context is { IsNewCustomer: true, Delivery: DeliveryType.Digital, Amount.Value: > AmountThreshold };
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? FormattableString.Invariant($"NEW_CUSTOMER_HIGH_AMOUNT: new customer with digital amount {context.Amount.Value}.")
                : "NEW_CUSTOMER_HIGH_AMOUNT: not triggered.");
    }
}
