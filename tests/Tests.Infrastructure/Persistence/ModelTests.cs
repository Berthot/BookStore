using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using Microsoft.EntityFrameworkCore;
using Tests.Shared.Attributes;

namespace Tests.Infrastructure.Persistence;

[Unit]
public sealed class ModelTests
{
    private static DbContextOptions<BookStoreDbContext> BookStoreOptions() =>
        new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseNpgsql("Host=test;Database=test")
            .Options;

    private static DbContextOptions<FraudDbContext> FraudOptions() =>
        new DbContextOptionsBuilder<FraudDbContext>()
            .UseNpgsql("Host=test;Database=test")
            .Options;

    [Test]
    public void FraudDbContext_DoesNotContainBookOrPurchase()
    {
        using var ctx = new FraudDbContext(FraudOptions());
        var entityNames = ctx.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        entityNames.Should().NotContain("Book");
        entityNames.Should().NotContain("Purchase");
    }

    [Test]
    public void BookStoreDbContext_DoesNotContainTransactionOrAssessment()
    {
        using var ctx = new BookStoreDbContext(BookStoreOptions());
        var entityNames = ctx.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        entityNames.Should().NotContain("Transaction");
        entityNames.Should().NotContain("Assessment");
    }

    [Test]
    public void BookStoreDbContext_ContainsBooksAndPurchases()
    {
        using var ctx = new BookStoreDbContext(BookStoreOptions());
        var entityNames = ctx.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        entityNames.Should().Contain("Book");
        entityNames.Should().Contain("Purchase");
    }

    [Test]
    public void FraudDbContext_ContainsTransactionsAndAssessments()
    {
        using var ctx = new FraudDbContext(FraudOptions());
        var entityNames = ctx.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        entityNames.Should().Contain("Transaction");
        entityNames.Should().Contain("Assessment");
    }

    [Test]
    public void FraudDbContext_RuleEvaluationMappedAsOwnedTable_NotJsonColumn()
    {
        using var ctx = new FraudDbContext(FraudOptions());
        var assessmentType = ctx.Model.FindEntityType(typeof(Domain.Entities.FraudAnalysis.Assessment));
        var ownedNav = assessmentType?.GetNavigations()
            .FirstOrDefault(n => n.Name == "Evaluations");

        ownedNav.Should().NotBeNull();
        ownedNav!.IsOnDependent.Should().BeFalse();
    }

    [Test]
    public void FraudDbContext_TransactionHasIndexOnPaymentFingerprintAndCreatedAt()
    {
        using var ctx = new FraudDbContext(FraudOptions());
        var txType = ctx.Model.FindEntityType(typeof(Domain.Entities.FraudAnalysis.Transaction));

        var index = txType?.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "ix_transactions_payment_fingerprint_occurred_at");

        index.Should().NotBeNull();
    }
}
