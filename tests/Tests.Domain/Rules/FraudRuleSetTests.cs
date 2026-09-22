using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Rules;
using Tests.Shared.Attributes;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Domain.Rules;

[Unit]
public sealed class FraudRuleSetTests
{
    private static readonly Guid TxId = TestConstants.Ids.TransactionId;
    private static readonly DateTime Now = TestConstants.Dates.FixedUtcNow;

    [Test]
    public void Evaluate_AllRulesMiss_ReturnsApprovedWithAllEvaluationsRecorded()
    {
        var rules = new[] { new StubRule("R1", false, 0.5m), new StubRule("R2", false, 0.5m) };
        var set = new FraudRuleSet(rules);

        var assessment = set.Evaluate(TxId, FraudContextMother.Default(), Now);

        assessment.Outcome.Should().Be(Outcome.Approved);
        assessment.Evaluations.Should().HaveCount(2);
        assessment.Evaluations.Should().AllSatisfy(e => e.Hit.Should().BeFalse());
    }

    [Test]
    public void Evaluate_NonHitRulesAreAlsoRecorded()
    {
        var rules = new[] { new StubRule("R_HIT", true, 0.6m), new StubRule("R_MISS", false, 0.5m) };
        var set = new FraudRuleSet(rules);

        var assessment = set.Evaluate(TxId, FraudContextMother.Default(), Now);

        assessment.Evaluations.Should().HaveCount(2);
        assessment.Evaluations.Should().ContainSingle(e => e.RuleCode == "R_MISS" && !e.Hit);
    }

    [Test]
    public void Evaluate_ScoreAboveRejectThreshold_ReturnsRejected()
    {
        var rules = new[] { new StubRule("R1", true, DecisionPolicy.RejectThreshold) };
        var set = new FraudRuleSet(rules);

        var assessment = set.Evaluate(TxId, FraudContextMother.Default(), Now);

        assessment.Outcome.Should().Be(Outcome.Rejected);
    }

    [Test]
    public void Evaluate_ScoreAtReviewThreshold_ReturnsReview()
    {
        var rules = new[] { new StubRule("R1", true, DecisionPolicy.ReviewThreshold) };
        var set = new FraudRuleSet(rules);

        var assessment = set.Evaluate(TxId, FraudContextMother.Default(), Now);

        assessment.Outcome.Should().Be(Outcome.Review);
    }

    [Test]
    public void Evaluate_SumsOnlyHitWeights()
    {
        // hit=0.3 + miss=0 = 0.3 < ReviewThreshold → Approved
        var rules = new[] { new StubRule("R_HIT", true, 0.3m), new StubRule("R_MISS", false, 0.9m) };
        var set = new FraudRuleSet(rules);

        var assessment = set.Evaluate(TxId, FraudContextMother.Default(), Now);

        assessment.Outcome.Should().Be(Outcome.Approved);
    }

    [Test]
    public void Evaluate_AssessmentTransactionIdMatchesInput()
    {
        var set = new FraudRuleSet([new StubRule("R1", false, 0m)]);
        var txId = Guid.NewGuid();

        var assessment = set.Evaluate(txId, FraudContextMother.Default(), Now);

        assessment.TransactionId.Should().Be(txId);
    }

    private sealed class StubRule(string code, bool hit, decimal weight) : IFraudRule
    {
        public string Code => code;
        public string Version => "1.0";

        public RuleEvaluation Evaluate(FraudContext context) =>
            new(code, Version, hit, hit ? weight : 0m, hit ? $"{code} triggered." : $"{code} not triggered.");
    }
}
