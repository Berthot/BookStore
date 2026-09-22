using Domain.Entities.FraudAnalysis;

namespace Tests.Shared.Mothers.FraudAnalysis;

public static class RuleEvaluationMother
{
    public static RuleEvaluation Hit(string code = "HIGH_VALUE_DIGITAL") =>
        new(code, "1.0", true, 0.6m, $"{code} triggered.");

    public static RuleEvaluation Miss(string code = "BULK_QUANTITY") =>
        new(code, "1.0", false, 0m, $"{code} not triggered.");
}
