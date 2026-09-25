using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.FraudAnalysis.SubmitTransaction;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.UseCases.FraudAnalysis.SubmitTransaction;

[Unit]
public sealed class SubmitTransactionHandlerTests : UnitTestsBase
{
    private ITransactionRepository _repo = null!;
    private IFraudUnitOfWork _uow = null!;
    private IEventPublisher _publisher = null!;
    private SubmitTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ITransactionRepository>();
        _uow = Substitute.For<IFraudUnitOfWork>();
        _publisher = Substitute.For<IEventPublisher>();
        _handler = new SubmitTransactionHandler(_repo, _uow, _publisher);
    }

    private static SubmitTransactionRequest ValidRequest(string correlationId = "corr-001") =>
        new(
            ExternalReference: "order-0001",
            CustomerId: "cus_001",
            AmountValue: 100m,
            AmountCurrency: "BRL",
            PaymentType: "CREDIT_CARD",
            PaymentFingerprint: "fp-0001",
            PaymentLast4: "4242",
            Channel: "WEB",
            Delivery: "DIGITAL",
            ItemCount: 1,
            OccurredAt: DateTime.UtcNow,
            CorrelationId: correlationId);

    [Test]
    public async Task Handle_valid_request_returns_success_with_received_status()
    {
        var result = await _handler.Handle(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(TransactionStatus.Received);
        result.Data.TransactionId.Should().NotBeEmpty();
    }

    [Test]
    public async Task Handle_valid_request_calls_commit_async_exactly_once()
    {
        await _handler.Handle(ValidRequest(), CancellationToken.None);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_valid_request_adds_transaction_and_publishes_event()
    {
        await _handler.Handle(ValidRequest(), CancellationToken.None);

        _repo.Received(1).Add(Arg.Any<Transaction>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionSubmitted>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_zero_amount_returns_validation_failure_without_side_effects()
    {
        var request = ValidRequest() with { AmountValue = 0m };

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Validation);
        _repo.DidNotReceive().Add(Arg.Any<Transaction>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_publish_occurs_before_commit_for_outbox_atomicity()
    {
        var callOrder = new List<string>();
        _publisher.When(x => x.PublishAsync(Arg.Any<TransactionSubmitted>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("publish"));
        _uow.When(x => x.CommitAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("commit"));

        await _handler.Handle(ValidRequest(), CancellationToken.None);

        // PublishAsync stages the message in the EF outbox; CommitAsync flushes both together.
        callOrder.Should().ContainInOrder("publish", "commit");
        callOrder.Should().HaveCount(2);
    }
}
