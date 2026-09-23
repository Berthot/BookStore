using Application.Commons;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Application.UseCases.FraudAnalysis.GetTransaction;

[Unit]
public sealed class GetTransactionHandlerTests : UnitTestsBase
{
    private ITransactionRepository _repo = null!;
    private GetTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ITransactionRepository>();
        _handler = new GetTransactionHandler(_repo);
    }

    [Test]
    public async Task Handle_unknown_id_returns_not_found()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.FraudAnalysis.Transaction?)null);

        var result = await _handler.Handle(new GetTransactionRequest(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_existing_transaction_returns_response_without_correlation_id()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(new GetTransactionRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TransactionId.Should().Be(transaction.Id);
        result.Data.Status.Should().Be("RECEIVED");

        var props = typeof(GetTransactionResponse).GetProperties().Select(p => p.Name);
        props.Should().NotContain("CorrelationId");
    }

    [Test]
    public async Task Handle_decided_transaction_includes_decision_and_history()
    {
        var transaction = TransactionMother.DecidedApproved();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(new GetTransactionRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Decision.Should().NotBeNull();
        result.Data.Decision!.Outcome.Should().Be("APPROVED");
        result.Data.History.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_received_transaction_decision_is_null()
    {
        var transaction = TransactionMother.Received();
        _repo.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _handler.Handle(new GetTransactionRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Decision.Should().BeNull();
        result.Data.History.Should().BeEmpty();
    }
}
