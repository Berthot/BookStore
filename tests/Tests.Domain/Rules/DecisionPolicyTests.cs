using Domain.Enums;
using Domain.Rules;
using Tests.Shared.Attributes;

namespace Tests.Domain.Rules;

[Unit]
public sealed class DecisionPolicyTests
{
    [Test]
    public void Decide_ScoreBelowReviewThreshold_ReturnsApproved()
    {
        var score = DecisionPolicy.ReviewThreshold - 0.01m;

        var outcome = DecisionPolicy.Decide(score);

        outcome.Should().Be(Outcome.Approved);
    }

    [Test]
    public void Decide_ScoreExactlyAtReviewThreshold_ReturnsReview()
    {
        var outcome = DecisionPolicy.Decide(DecisionPolicy.ReviewThreshold);

        outcome.Should().Be(Outcome.Review);
    }

    [Test]
    public void Decide_ScoreAboveReviewThresholdBelowRejectThreshold_ReturnsReview()
    {
        var score = DecisionPolicy.RejectThreshold - 0.01m;

        var outcome = DecisionPolicy.Decide(score);

        outcome.Should().Be(Outcome.Review);
    }

    [Test]
    public void Decide_ScoreExactlyAtRejectThreshold_ReturnsRejected()
    {
        var outcome = DecisionPolicy.Decide(DecisionPolicy.RejectThreshold);

        outcome.Should().Be(Outcome.Rejected);
    }

    [Test]
    public void Decide_ScoreAboveRejectThreshold_ReturnsRejected()
    {
        var score = DecisionPolicy.RejectThreshold + 0.01m;

        var outcome = DecisionPolicy.Decide(score);

        outcome.Should().Be(Outcome.Rejected);
    }

    [Test]
    public void Decide_ScoreZero_ReturnsApproved()
    {
        var outcome = DecisionPolicy.Decide(0m);

        outcome.Should().Be(Outcome.Approved);
    }
}
