using Domain.Enums;

namespace Domain.Rules;

public static class DecisionPolicy
{
    /// <summary>Score at or above this value results in Rejected. Calibrated to catch high-confidence fraud signals where two or more strong rules align.</summary>
    public const decimal RejectThreshold = 0.7m;

    /// <summary>Score at or above this value (and below RejectThreshold) triggers manual Review. Covers borderline cases where human oversight reduces false positives.</summary>
    public const decimal ReviewThreshold = 0.4m;

    /// <summary>Converts a cumulative risk score into an Outcome using the configured thresholds.</summary>
    public static Outcome Decide(decimal score) =>
        score >= RejectThreshold ? Outcome.Rejected :
        score >= ReviewThreshold ? Outcome.Review :
        Outcome.Approved;
}
