using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Application.UseCases.FraudAnalysis.ReviewTransaction;
using Domain.Entities.FraudAnalysis;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Application.UseCases.FraudAnalysis.ReviewTransaction;

[Unit]
public sealed class ReviewTransactionHandlerTests : UnitTestsBase
{
    private ITransactionRepository _repo = null!;
    private IFraudUnitOfWork _uow = null!;
    private IEventPublisher _publisher = null!;
    private ReviewTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ITransactionRepository>();
        _uow = Substitute.For<IFraudUnitOfWork>();
        _publisher = Substitute.For<IEventPublisher>();
        _handler = new ReviewTransactionHandler(_repo, _uow, _publisher);
    }

    private static ReviewTransactionRequest ApproveRequest(Guid id) =>
        new(id, "APPROVED", "Reviewed and approved.", "reviewer-001");

    [Test]
    public async Task Handle_unknown_transaction_returns_not_found()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _handler.Handle(ApproveRequest(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_transaction_not_pending_review_returns_conflict()
    {
        var transaction = TransactionMother.DecidedApproved();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(ApproveRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
    }

    [Test]
    public async Task Handle_empty_justification_returns_unprocessable()
    {
        var transaction = TransactionMother.DecidedReview();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        var request = new ReviewTransactionRequest(transaction.Id, "APPROVED", string.Empty, null);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unprocessable);
    }

    [Test]
    public async Task Handle_invalid_outcome_returns_unprocessable()
    {
        var transaction = TransactionMother.DecidedReview();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        var request = new ReviewTransactionRequest(transaction.Id, "UNKNOWN", "Reason.", null);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unprocessable);
    }

    [Test]
    public async Task Handle_valid_review_records_assessment_and_keeps_history()
    {
        var transaction = TransactionMother.DecidedReview();
        var previousCount = transaction.Assessments.Count;
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(ApproveRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeOfType<GetTransactionResponse>();
        result.Data!.History.Should().HaveCount(previousCount + 1);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>());
    }
}
