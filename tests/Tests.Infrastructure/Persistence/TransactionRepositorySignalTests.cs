using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Fraud;
using Infrastructure.Persistence.Fraud.Repositories;
using Tests.Shared.Attributes;

namespace Tests.Infrastructure.Persistence;

/// <summary>
/// Verifica que as queries de sinal do antifraude contam apenas transações com
/// OccurredAt estritamente anterior à transação avaliada (D-44).
///
/// Sem o filtro `until`, a avaliação da 3ª compra do 05S contava as compras 4–6
/// (ainda não avaliadas mas já gravadas), disparando CARD_VELOCITY indevidamente.
/// </summary>
[Integration]
public sealed class TransactionRepositorySignalTests
{
    private string _cs = null!;

    [SetUp]
    public void SetUp()
    {
        if (string.IsNullOrEmpty(PostgresContainerFixture.ConnectionString))
            Assert.Ignore($"Docker not available — {PostgresContainerFixture.StartupException?.GetType().Name}: {PostgresContainerFixture.StartupException?.Message ?? "no details"}");

        _cs = PostgresContainerFixture.ConnectionString;
    }

    // Insere `count` transações com o mesmo cartão, 1 s de intervalo a partir de baseTime.
    private static async Task<List<Transaction>> SeedSameCardTransactionsAsync(
        FraudDbContext ctx, string fingerprint, string customerId, int count, DateTime baseTime)
    {
        var transactions = Enumerable.Range(0, count)
            .Select(i => new Transaction
            {
                Id = Guid.NewGuid(),
                ExternalReference = $"ref-card-{i}",
                CustomerId = customerId,
                Amount = new Money(49.90m, "BRL"),
                Channel = Channel.Web,
                DeliveryType = DeliveryType.Physical,
                Payment = new PaymentInstrument("credit_card", fingerprint, "0000"),
                PaymentFingerprint = fingerprint,
                ItemCount = 1,
                OccurredAt = baseTime.AddSeconds(i),
                CorrelationId = $"corr-{fingerprint}-{i}-{Guid.NewGuid():N}",
                CreatedAt = baseTime.AddSeconds(i)
            })
            .ToList();

        ctx.Transactions.AddRange(transactions);
        await ctx.SaveChangesAsync();
        return transactions;
    }

    // --- CARD_VELOCITY ---

    [Test]
    public async Task CardVelocity_counts_only_purchases_before_evaluated_one()
    {
        // Cenário 05S: 6 compras com o mesmo cartão gravadas antes de qualquer avaliação.
        // Ao avaliar a 3ª, só as 2 anteriores (t[0] e t[1]) devem ser contadas.
        var baseTime = DateTime.UtcNow.AddDays(-1);
        var fp = $"fp-vel3-{Guid.NewGuid():N}";
        var cus = $"cus-vel3-{Guid.NewGuid():N}";

        await using var ctx = PostgresContainerFixture.BuildFraudContext(_cs);
        var txs = await SeedSameCardTransactionsAsync(ctx, fp, cus, 6, baseTime);

        var repo = new TransactionRepository(ctx);
        var count = await repo.CountRecentByFingerprintAsync(
            fp, txs[2].OccurredAt.AddDays(-30), txs[2].OccurredAt, txs[2].Id);

        count.Should().Be(2,
            "ao avaliar a 3ª compra, apenas as 2 anteriores devem ser contadas — as 3 posteriores não existiam ainda");
    }

    [Test]
    public async Task CardVelocity_fires_correctly_on_sixth_purchase()
    {
        var baseTime = DateTime.UtcNow.AddDays(-1);
        var fp = $"fp-vel6-{Guid.NewGuid():N}";
        var cus = $"cus-vel6-{Guid.NewGuid():N}";

        await using var ctx = PostgresContainerFixture.BuildFraudContext(_cs);
        var txs = await SeedSameCardTransactionsAsync(ctx, fp, cus, 6, baseTime);

        var repo = new TransactionRepository(ctx);
        var count = await repo.CountRecentByFingerprintAsync(
            fp, txs[5].OccurredAt.AddDays(-30), txs[5].OccurredAt, txs[5].Id);

        count.Should().Be(5,
            "ao avaliar a 6ª compra, as 5 anteriores devem ser contadas e disparar CARD_VELOCITY");
    }

    // --- IsNewCustomer / CustomerStats ---

    [Test]
    public async Task CustomerStats_excludes_later_transactions_so_first_purchase_is_new_customer()
    {
        // T1 e T2 são do mesmo cliente mas T2 é posterior.
        // Ao avaliar T1, o count deve ser 0 (IsNewCustomer = true).
        // Ao avaliar T2, o count deve ser 1 (cliente já existe).
        var baseTime = DateTime.UtcNow.AddDays(-1);
        var cus = $"cus-new-{Guid.NewGuid():N}";
        var fp = $"fp-new-{Guid.NewGuid():N}";

        var t1 = new Transaction
        {
            Id = Guid.NewGuid(), ExternalReference = "ref-n1", CustomerId = cus,
            Amount = new Money(100m, "BRL"), Channel = Channel.Web, DeliveryType = DeliveryType.Physical,
            Payment = new PaymentInstrument("credit_card", fp, "0000"),
            PaymentFingerprint = fp, ItemCount = 1,
            OccurredAt = baseTime,
            CorrelationId = $"corr-n1-{Guid.NewGuid():N}", CreatedAt = baseTime
        };
        var t2 = new Transaction
        {
            Id = Guid.NewGuid(), ExternalReference = "ref-n2", CustomerId = cus,
            Amount = new Money(200m, "BRL"), Channel = Channel.Web, DeliveryType = DeliveryType.Physical,
            Payment = new PaymentInstrument("credit_card", fp, "0000"),
            PaymentFingerprint = fp, ItemCount = 1,
            OccurredAt = baseTime.AddSeconds(10),
            CorrelationId = $"corr-n2-{Guid.NewGuid():N}", CreatedAt = baseTime.AddSeconds(10)
        };

        await using var ctx = PostgresContainerFixture.BuildFraudContext(_cs);
        ctx.Transactions.AddRange(t1, t2);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);

        var (count1, _) = await repo.GetCustomerStatsAsync(cus, t1.OccurredAt, t1.Id);
        count1.Should().Be(0,
            "T2 é posterior a T1 e não deve ser contada; T1 não tem histórico anterior");

        var (count2, avg2) = await repo.GetCustomerStatsAsync(cus, t2.OccurredAt, t2.Id);
        count2.Should().Be(1, "T1 é anterior a T2 e deve ser incluída");
        avg2.Should().Be(100m, "média do histórico de T2 é o valor de T1 (R$ 100)");
    }
}
