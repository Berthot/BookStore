using Application.Abstractions.Messaging;
using Application.Messages;
using Application.UseCases.Sales.ApplyFraudDecision;
using Application.UseCases.FraudAnalysis.ReviewTransaction;
using Domain.Entities.FraudAnalysis;
using Domain.Entities.Sales;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Infrastructure.UseCases;

/// <summary>
/// Integration tests for the review flow: ReviewTransaction → TransactionDecided → ApplyFraudDecision.
/// Runs against a real PostgreSQL container (via PostgresContainerFixture).
/// Uses a bridging event publisher to simulate the bus delivery without requiring RabbitMQ.
/// </summary>
[Integration]
public sealed class ReviewDecisionIntegrationTests
{
    private IServiceProvider _sp = null!;
    private IServiceScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        if (string.IsNullOrEmpty(PostgresContainerFixture.ConnectionString))
            Assert.Ignore($"Docker not available — {PostgresContainerFixture.StartupException?.GetType().Name}: {PostgresContainerFixture.StartupException?.Message ?? "no details"}");

        var connStr = PostgresContainerFixture.ConnectionString;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:BookStore"] = connStr,
                [$"ConnectionStrings:Fraud"] = connStr,
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Seeding:Enabled"] = "false",
                ["Reconciliation:LookbackSeconds"] = "300",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBookStorePersistence(config);
        services.AddFraudPersistence(config);

        _sp = services.BuildServiceProvider();
        _scope = _sp.CreateScope();
    }

    [TearDown]
    public void TearDown()
    {
        _scope?.Dispose();
        (_sp as IDisposable)?.Dispose();
    }

    /// <summary>Full happy path: engine sends Review → reviewer APPROVES → purchase becomes CONFIRMED with decidedBy REVIEWER and engine rules preserved.</summary>
    [Test]
    public async Task ReviewApproved_updates_purchase_to_CONFIRMED_with_REVIEWER_decider_and_engine_rules()
    {
        var correlationId = $"review-approved-{Guid.NewGuid()}";
        var transactionId = await SetUpUnderReviewAsync(correlationId);

        // Capture TransactionDecided and bridge to ApplyFraudDecisionHandler
        var publisher = Substitute.For<IEventPublisher>();
        TransactionDecided? captured = null;
        publisher.When(x => x.PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<TransactionDecided>());

        var fraudRepo = _scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var fraudUow = _scope.ServiceProvider.GetRequiredService<IFraudUnitOfWork>();
        var reviewHandler = new ReviewTransactionHandler(fraudRepo, fraudUow, publisher);

        var result = await reviewHandler.Handle(
            new ReviewTransactionRequest(transactionId, "APPROVED", "Looks clean, approved.", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull("review handler must publish TransactionDecided");
        captured!.Outcome.Should().Be(Outcome.Approved);
        captured.DecidedBy.Should().Be("REVIEWER");

        // Bridge: simulate message delivery
        await ApplyDecisionAsync(captured);

        // Assert purchase state
        var purchaseRepo = _scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var purchase = await purchaseRepo.GetByTransactionIdAsync(transactionId, CancellationToken.None);

        purchase.Should().NotBeNull();
        purchase!.Status.Should().Be(PurchaseStatus.Confirmed, "APPROVED review must transition purchase to Confirmed");
        purchase.FraudOutcome.Should().NotBeNull();
        purchase.FraudOutcome!.DecidedBy.Should().Be("REVIEWER", "the reviewer's decision must be recorded");
        purchase.FraudOutcome.TriggeredRules.Should().NotBeEmpty("engine rules must be carried over from the original assessment");
    }

    /// <summary>Reviewer REJECTS → purchase becomes CANCELLED.</summary>
    [Test]
    public async Task ReviewRejected_updates_purchase_to_CANCELLED()
    {
        var correlationId = $"review-rejected-{Guid.NewGuid()}";
        var transactionId = await SetUpUnderReviewAsync(correlationId);

        var publisher = Substitute.For<IEventPublisher>();
        TransactionDecided? captured = null;
        publisher.When(x => x.PublishAsync(Arg.Any<TransactionDecided>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<TransactionDecided>());

        var fraudRepo = _scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var fraudUow = _scope.ServiceProvider.GetRequiredService<IFraudUnitOfWork>();
        var reviewHandler = new ReviewTransactionHandler(fraudRepo, fraudUow, publisher);

        var result = await reviewHandler.Handle(
            new ReviewTransactionRequest(transactionId, "REJECTED", "Multiple high-risk signals.", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Outcome.Should().Be(Outcome.Rejected);

        await ApplyDecisionAsync(captured);

        var purchaseRepo = _scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var purchase = await purchaseRepo.GetByTransactionIdAsync(transactionId, CancellationToken.None);

        purchase.Should().NotBeNull();
        purchase!.Status.Should().Be(PurchaseStatus.Cancelled, "REJECTED review must transition purchase to Cancelled");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Persists a Transaction in Decided/Review state and a Purchase in UnderReview linked to it.
    /// Returns the transaction ID.
    /// </summary>
    private async Task<Guid> SetUpUnderReviewAsync(string correlationId)
    {
        var fraudRepo = _scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var fraudUow = _scope.ServiceProvider.GetRequiredService<IFraudUnitOfWork>();
        var purchaseRepo = _scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var bsUow = _scope.ServiceProvider.GetRequiredService<IBookStoreUnitOfWork>();

        // Build a Transaction that is Decided with Outcome.Review
        var now = DateTime.UtcNow;
        var (tx, _) = Transaction.Create(
            $"order-{correlationId}",
            "cus-integration-test",
            new Money(150m, "BRL"),
            Channel.Web,
            DeliveryType.Digital,
            new PaymentInstrument("CREDIT_CARD", $"fp-{correlationId}", "4242"),
            10,
            now,
            correlationId,
            now);

        tx!.StartProcessing();

        // Engine assessment with a rule hit so we can verify carry-over
        var engineAssessment = Assessment.ForEngine(
            tx.Id,
            Outcome.Review,
            [new RuleEvaluation("BULK_QTY", "1.0", true, 1.0m, "Bulk quantity threshold exceeded", 1)],
            now);
        tx.Decide(engineAssessment);

        fraudRepo.Add(tx);
        fraudRepo.AddAssessment(engineAssessment);
        await fraudUow.CommitAsync(CancellationToken.None);

        // Create a Purchase linked to this transaction, already in UnderReview
        var purchase = Purchase.Create(
            Guid.NewGuid(), "Ebook", 10,
            new Money(150m, "BRL"),
            "cus-integration-test", "CREDIT_CARD", $"fp-{correlationId}", "4242",
            correlationId, now);

        purchase.LinkTransaction(tx.Id);
        purchase.ApplyDecision(Outcome.Review);
        purchaseRepo.Add(purchase);
        await bsUow.CommitAsync(CancellationToken.None);

        return tx.Id;
    }

    private async Task ApplyDecisionAsync(TransactionDecided decision)
    {
        var purchaseRepo = _scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var bsUow = _scope.ServiceProvider.GetRequiredService<IBookStoreUnitOfWork>();
        var applyHandler = new ApplyFraudDecisionHandler(purchaseRepo, bsUow);

        await applyHandler.Handle(new ApplyFraudDecisionRequest(
            decision.TransactionId,
            decision.Outcome,
            decision.Score,
            decision.DecidedBy,
            decision.TriggeredRules), CancellationToken.None);
    }
}
