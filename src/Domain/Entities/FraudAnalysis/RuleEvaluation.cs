namespace Domain.Entities.FraudAnalysis;

public sealed record RuleEvaluation(
    string RuleCode,
    string RuleVersion,
    bool Hit,
    decimal Weight,
    string Reason);
