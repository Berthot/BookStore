using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.AssessTransaction;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Repositories;
using Domain.Rules;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Application.UseCases.FraudAnalysis.AssessTransaction;

[Unit]
public sealed class AssessTransactionHandlerTests : UnitTestsBase
{
    private ITransactionRepository _repo = null!;
    private IFraudUnitOfWork _uow = null!;
    private IEventPublisher _publisher = null!;
    private AssessTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ITransactionRepository>();
        _uow = Substitute.For<IFraudUnitOfWork>();
        _publisher = Substitute.For<IEventPublisher>();
        var noopRule = Substitute.For<IFraudRule>();
        noopRule.Evaluate(Arg.Any<FraudContext>())
            .Returns(new RuleEvaluation("NOOP", "1.0", false, 0m, "no-op"));
        _handler = new AssessTransactionHandler(_repo, _uow, _publisher, new FraudRuleSet([noopRule]));
    }

    [Test]
    public async Task Handle_unknown_transaction_returns_not_found()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _handler.Handle(new AssessTransactionRequest(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_already_decided_transaction_returns_success_without_side_effects()
    {
        var transaction = TransactionMother.DecidedApproved();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(new AssessTransactionRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Outcome.Should().Be(Outcome.Approved);
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_received_transaction_commits_twice_and_publishes_event()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        await _handler.Handle(new AssessTransactionRequest(transaction.Id), CancellationToken.None);

        await _uow.Received(2).CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_commit_occurs_before_publish()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var order = new List<string>();
        _publisher.When(x => x.PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("publish"));
        _uow.When(x => x.CommitAsync(Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("commit"));

        await _handler.Handle(new AssessTransactionRequest(transaction.Id), CancellationToken.None);

        // commit(processing), commit(decided), publish(decided)
        order.Should().ContainInOrder("commit", "commit", "publish");
        order.Should().HaveCount(3);
    }

    [Test]
    public async Task Handle_signals_are_queried_from_repository()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        await _handler.Handle(new AssessTransactionRequest(transaction.Id), CancellationToken.None);

        await _repo.Received(1).CountRecentByFingerprintAsync(
            transaction.PaymentFingerprint, Arg.Any<DateTime>(), transaction.Id, Arg.Any<CancellationToken>());
        await _repo.Received(1).GetCustomerStatsAsync(
            transaction.CustomerId, transaction.Id, Arg.Any<CancellationToken>());
        await _repo.Received(1).CountJustBelowThresholdAsync(
            transaction.CustomerId, Arg.Any<decimal>(), Arg.Any<decimal>(),
            Arg.Any<DateTime>(), transaction.Id, Arg.Any<CancellationToken>());
    }
}
