using Domain.Entities.FraudAnalysis;

namespace Domain.Rules.Transactional;

public sealed class CardVelocityRule : IFraudRule
{
    public string Code => "CARD_VELOCITY";
    public string Version => "1.0";

    /// <summary>Number of recent transactions with the same card fingerprint that triggers this rule (S5).</summary>
    public const int RecentTransactionThreshold = 5;

    /// <summary>Risk weight applied when the rule fires. Alone meets RejectThreshold, flagging stolen-card patterns (S5).</summary>
    public const decimal Weight = 0.7m;

    public RuleEvaluation Evaluate(FraudContext context)
    {
        var hit = context.RecentTransactionsWithSameCard >= RecentTransactionThreshold;
        return new RuleEvaluation(Code, Version, hit, hit ? Weight : 0m,
            hit ? $"CARD_VELOCITY: {context.RecentTransactionsWithSameCard} recent transactions with same card."
                : "CARD_VELOCITY: not triggered.");
    }
}
