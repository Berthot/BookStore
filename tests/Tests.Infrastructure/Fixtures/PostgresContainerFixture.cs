using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Tests.Infrastructure.Fixtures;

/// <summary>Shared PostgreSQL container that starts once per test session, applies both EF migrations, and exposes the connection string.</summary>
[SetUpFixture]
public sealed class PostgresContainerFixture
{
    private static PostgreSqlContainer _container = null!;

    public static string ConnectionString { get; private set; } = "";

    [OneTimeSetUp]
    public async Task StartContainerAndMigrateAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("bookstore_test")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();

            await using var bookStoreCtx = BuildBookStoreContext(ConnectionString);
            await bookStoreCtx.Database.MigrateAsync();

            await using var fraudCtx = BuildFraudContext(ConnectionString);
            await fraudCtx.Database.MigrateAsync();
        }
        catch (Exception ex) when (ex is DotNet.Testcontainers.Builders.DockerUnavailableException
                                      or System.AggregateException { InnerException: DotNet.Testcontainers.Builders.DockerUnavailableException })
        {
            // Docker not available in this environment — integration tests will be ignored, unit tests unaffected
        }
    }

    [OneTimeTearDown]
    public async Task StopContainerAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public static BookStoreDbContext BuildBookStoreContext(string connectionString)
    {
        var opts = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure()
                      .MigrationsHistoryTable("__EFMigrationsHistory", "bookstore"))
            .Options;
        return new BookStoreDbContext(opts);
    }

    public static FraudDbContext BuildFraudContext(string connectionString)
    {
        var opts = new DbContextOptionsBuilder<FraudDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure()
                      .MigrationsHistoryTable("__EFMigrationsHistory", "fraud"))
            .Options;
        return new FraudDbContext(opts);
    }
}
