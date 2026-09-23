using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.FailSafe;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Application.UseCases.FraudAnalysis.FailSafe;

[Unit]
public sealed class FailSafeTransactionHandlerTests : UnitTestsBase
{
    private ITransactionRepository _repo = null!;
    private IFraudUnitOfWork _uow = null!;
    private IEventPublisher _publisher = null!;
    private FailSafeTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ITransactionRepository>();
        _uow = Substitute.For<IFraudUnitOfWork>();
        _publisher = Substitute.For<IEventPublisher>();
        _handler = new FailSafeTransactionHandler(_repo, _uow, _publisher);
    }

    private static FailSafeTransactionRequest Request(Guid id) =>
        new(id, "Processing unavailable after maximum retry attempts.");

    [Test]
    public async Task Handle_fail_safe_always_produces_review_outcome()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(Request(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Outcome.Should().Be(Outcome.Review);
    }

    [Test]
    public async Task Handle_already_decided_returns_success_without_side_effects()
    {
        var transaction = TransactionMother.DecidedApproved();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(Request(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_processing_transaction_commits_and_publishes_review()
    {
        var transaction = TransactionMother.Processing();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(Request(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Outcome.Should().Be(Outcome.Review);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>());
    }
}
