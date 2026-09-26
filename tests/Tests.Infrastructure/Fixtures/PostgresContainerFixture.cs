using System.Net.Sockets;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Infrastructure;

/// <summary>Shared PostgreSQL container that starts once per test session, applies both EF migrations, and exposes the connection string.</summary>
[SetUpFixture]
public sealed class PostgresContainerFixture
{
    private static PostgreSqlContainer _container = null!;

    public static string ConnectionString { get; private set; } = "";
    public static Exception? StartupException { get; private set; }

    [OneTimeSetUp]
    public async Task StartContainerAndMigrateAsync()
    {
        try
        {
            // Ryuk from a previous test run may still be alive when the next process starts,
            // causing ResourceReaperException on StartAsync. [OneTimeTearDown] disposes the
            // container explicitly so Ryuk is not needed for this fixture.
            DotNet.Testcontainers.Configurations.TestcontainersSettings.ResourceReaperEnabled = false;

            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("bookstore_test")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            await _container.StartAsync();

            // SSL Mode=Disable: local container has no TLS; without it Npgsql attempts a TLS
            // SetupEncryption handshake that times out on Rancher Desktop/WSL because the port
            // accepts connections before Postgres is ready to negotiate.
            var cs = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                SslMode = SslMode.Disable
            }.ToString();

            // Rancher Desktop/WSL: the mapped port is reachable before Postgres finishes startup.
            // TCP probe waits for host-side port-forwarding to be active before running migrations.
            await WaitForPostgresReadyAsync(cs);

            ConnectionString = cs;

            await using var bookStoreCtx = BuildBookStoreContext(ConnectionString);
            await bookStoreCtx.Database.MigrateAsync();

            await using var fraudCtx = BuildFraudContext(ConnectionString);
            await fraudCtx.Database.MigrateAsync();
        }
        catch (Exception ex) when (ex is DotNet.Testcontainers.Builders.DockerUnavailableException
                                      or System.AggregateException { InnerException: DotNet.Testcontainers.Builders.DockerUnavailableException })
        {
            StartupException = ex;
        }
    }

    // Raw TCP probe: bypasses Npgsql protocol negotiation (which on Rancher Desktop/WSL can hang
    // even after pg_isready passes) and only checks that the host-side port-forwarding is active.
    // The migrations themselves use EnableRetryOnFailure — they tolerate the first connection
    // being slightly late without a custom probe.
    private static async Task WaitForPostgresReadyAsync(string connectionString)
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        var host = csb.Host ?? "localhost";
        var port = csb.Port > 0 ? csb.Port : 5432;

        const int maxAttempts = 20;
        const int intervalMs = 500;

        Exception? lastException = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port, cts.Token);
                return; // Port is reachable from the host
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(intervalMs);
            }
        }

        throw new InvalidOperationException(
            $"PostgreSQL port {host}:{port} not reachable after {maxAttempts} attempts ({maxAttempts * (2000 + intervalMs) / 1000.0:F1} s max).",
            lastException);
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
