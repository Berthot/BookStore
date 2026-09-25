using Application.Abstractions.Messaging;
using Application.Messages;
using Application.UseCases.FraudAnalysis.ReviewTransaction;
using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Fraud;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tests.Infrastructure.Fixtures;
using Tests.Shared.Attributes;

namespace Tests.Infrastructure.Persistence;

/// <summary>
/// Proves that ReviewTransactionHandler stages the TransactionDecided message in the EF Core outbox
/// atomically with the domain entity update.
///
/// Key invariant: PublishAsync (stages in outbox) MUST be called BEFORE CommitAsync (SaveChangesAsync),
/// so both the reviewer assessment AND the OutboxMessage are flushed in the same transaction.
/// If the order were reversed — CommitAsync before PublishAsync — the OutboxMessage would never
/// be persisted and the downstream ApplyFraudDecision handler would never fire.
/// </summary>
[Integration]
public sealed class OutboxIntegrationTests
{
    private string _connStr = null!;

    [SetUp]
    public void SetUp()
    {
        if (string.IsNullOrEmpty(PostgresContainerFixture.ConnectionString))
            Assert.Ignore("Docker not available — skipping integration test");

        _connStr = PostgresContainerFixture.ConnectionString;
    }

    [Test]
    [CancelAfter(30_000)]
    public async Task ReviewTransaction_stages_outbox_message_atomically_with_domain_changes(CancellationToken cancellationToken)
    {
        IBusControl? bus = null;
        ServiceProvider? provider = null;

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Fraud"] = _connStr,
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                })
                .Build();

            services.AddFraudPersistence(config);

            // MassTransit: in-memory transport with EF outbox — no RabbitMQ needed
            services.AddMassTransit(b =>
            {
                b.AddEntityFrameworkOutbox<FraudDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();
                });
                // In-memory transport: no broker needed; messages won't be consumed (no consumers registered)
                b.UsingInMemory();
            });

            // Test-local publisher wrapping the MassTransit IPublishEndpoint
            services.AddScoped<IEventPublisher>(sp =>
                new TestEventPublisher(sp.GetRequiredService<IPublishEndpoint>()));

            provider = services.BuildServiceProvider();

            bus = provider.GetRequiredService<IBusControl>();
            await bus.StartAsync(CancellationToken.None);

            Guid transactionId;

            // ── set up a UNDER_REVIEW transaction in the real DB ──────────────────
            {
                await using var setupScope = provider.CreateAsyncScope();
                var fraudCtx = setupScope.ServiceProvider.GetRequiredService<FraudDbContext>();
                var now = DateTime.UtcNow;

                var (tx, _) = Transaction.Create(
                    "order-outbox-test",
                    "cus-outbox",
                    new Money(200m, "BRL"),
                    Channel.Web,
                    DeliveryType.Digital,
                    new PaymentInstrument("CREDIT_CARD", "fp-outbox", "4242"),
                    5,
                    now,
                    $"corr-outbox-{Guid.NewGuid()}",
                    now);

                tx!.StartProcessing();
                var engineAssessment = Assessment.ForEngine(
                    tx.Id,
                    Outcome.Review,
                    [new RuleEvaluation("BULK_QTY", "1.0", true, 0.8m, "Bulk", 1)],
                    now);
                tx.Decide(engineAssessment);
                fraudCtx.Transactions.Add(tx);
                fraudCtx.Assessments.Add(engineAssessment);
                await fraudCtx.SaveChangesAsync(CancellationToken.None);
                transactionId = tx.Id;
            }

            // Count outbox messages BEFORE calling the handler
            var outboxCountBefore = await CountOutboxMessagesAsync();

            // ── call ReviewTransactionHandler ──────────────────────────────────────
            {
                await using var handlerScope = provider.CreateAsyncScope();
                var txRepo = handlerScope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                var uow = handlerScope.ServiceProvider.GetRequiredService<IFraudUnitOfWork>();
                var publisher = handlerScope.ServiceProvider.GetRequiredService<IEventPublisher>();
                var handler = new ReviewTransactionHandler(txRepo, uow, publisher);

                var result = await handler.Handle(
                    new ReviewTransactionRequest(transactionId, "APPROVED", "Outbox test approval.", null),
                    CancellationToken.None);

                result.IsSuccess.Should().BeTrue("handler must succeed");
            }

            // ── assert OutboxMessage row was staged atomically ─────────────────────
            var outboxCountAfter = await CountOutboxMessagesAsync();

            outboxCountAfter.Should().BeGreaterThan(outboxCountBefore,
                "ReviewTransactionHandler must stage a TransactionDecided OutboxMessage " +
                "in the same transaction as the reviewer Assessment — " +
                "if the outbox row is missing, PublishAsync was called after CommitAsync (the bug).");
        }
        finally
        {
            if (bus is not null)
                await bus.StopAsync(CancellationToken.None);
            provider?.Dispose();
        }
    }

    private async Task<long> CountOutboxMessagesAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("""SELECT COUNT(*) FROM fraud."OutboxMessage" """, conn);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    // Test-local publisher: replicates what Infrastructure.Messaging.EventPublisher does (internal type)
    private sealed class TestEventPublisher(IPublishEndpoint endpoint) : IEventPublisher
    {
        public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class =>
            endpoint.Publish(message, ct);
    }
}
