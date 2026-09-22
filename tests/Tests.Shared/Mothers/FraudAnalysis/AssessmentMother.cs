using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Tests.Shared.Constants;

namespace Tests.Shared.Mothers.FraudAnalysis;

public static class AssessmentMother
{
    public static Assessment EngineApproved(Guid? transactionId = null) =>
        Assessment.ForEngine(
            transactionId ?? TestConstants.Ids.TransactionId,
            Outcome.Approved,
            [RuleEvaluationMother.Miss()],
            TestConstants.Dates.FixedUtcNow);

    public static Assessment EngineRejected(Guid? transactionId = null) =>
        Assessment.ForEngine(
            transactionId ?? TestConstants.Ids.TransactionId,
            Outcome.Rejected,
            [RuleEvaluationMother.Hit()],
            TestConstants.Dates.FixedUtcNow);

    public static Assessment EngineReview(Guid? transactionId = null) =>
        Assessment.ForEngine(
            transactionId ?? TestConstants.Ids.TransactionId,
            Outcome.Review,
            [RuleEvaluationMother.Hit()],
            TestConstants.Dates.FixedUtcNow);

    public static Assessment ReviewerApproved(Guid? transactionId = null) =>
        Assessment.ForReviewer(
            transactionId ?? TestConstants.Ids.TransactionId,
            Outcome.Approved,
            "Reviewed and approved by analyst.",
            "reviewer-001",
            TestConstants.Dates.FixedUtcNow.AddMinutes(5));

    public static Assessment ReviewerRejected(Guid? transactionId = null) =>
        Assessment.ForReviewer(
            transactionId ?? TestConstants.Ids.TransactionId,
            Outcome.Rejected,
            "Reviewed and rejected by analyst.",
            "reviewer-001",
            TestConstants.Dates.FixedUtcNow.AddMinutes(5));
}
