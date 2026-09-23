using Application.Abstractions.Idempotency;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.Shared.Attributes;

namespace Tests.Infrastructure.Persistence;

/// <summary>
/// Integration tests for idempotency persistence behaviour.
/// Uses a real PostgreSQL container (via PostgresContainerFixture) — no in-memory simulation.
/// Tests verify the unique index on fraud.idempotency_keys(key) and the lock_timeout behaviour.
///
/// NOTE: These tests would PASS (incorrectly) if the unique index were removed from
/// IdempotencyEntryConfiguration, because no exception would be thrown on the second insert.
/// The assertions are designed to detect exactly that regression.
/// </summary>
[Integration]
public sealed class IdempotencyIntegrationTests
{
    private string _cs = null!;

    [SetUp]
    public void SetUp()
    {
        if (string.IsNullOrEmpty(PostgresContainerFixture.ConnectionString))
            Assert.Ignore("Docker not available — skipping integration test");

        _cs = PostgresContainerFixture.ConnectionString;
    }

    // --- Unique index tests (TSK-0088) ---

    [Test]
    public async Task Unique_index_rejects_second_insert_with_same_key()
    {
        // CRITICAL: if the unique index ix_fraud_idempotency_keys_key is removed, SaveChangesAsync
        // on the second context will succeed — making this assertion FAIL and exposing the regression.
        var key = $"idem-unique-{Guid.NewGuid()}";

        await using var ctx1 = PostgresContainerFixture.BuildFraudContext(_cs);
        ctx1.Set<IdempotencyEntry>().Add(IdempotencyEntry.Create(key, "hash-a", DateTime.UtcNow));
        await ctx1.SaveChangesAsync();

        await using var ctx2 = PostgresContainerFixture.BuildFraudContext(_cs);
        ctx2.Set<IdempotencyEntry>().Add(IdempotencyEntry.Create(key, "hash-b", DateTime.UtcNow));

        var act = async () => await ctx2.SaveChangesAsync();
        var thrown = await act.Should().ThrowAsync<DbUpdateException>(
            "the unique index must prevent a second insert with the same key");
        var pgEx = thrown.Which.InnerException as PostgresException;
        pgEx.Should().NotBeNull("inner exception must be a PostgresException");
        pgEx!.SqlState.Should().Be("23505", "SQL state 23505 = unique_violation");
    }

    [Test]
    public async Task Entry_lifecycle_add_find_complete_persists_correctly()
    {
        var key = $"idem-lifecycle-{Guid.NewGuid()}";
        const string hash = "sha256-abc";
        const string responseBody = """{"id":"test"}""";
        var resourceId = Guid.NewGuid();

        await using var ctx1 = PostgresContainerFixture.BuildFraudContext(_cs);
        var entry = IdempotencyEntry.Create(key, hash, DateTime.UtcNow);
        ctx1.Set<IdempotencyEntry>().Add(entry);
        await ctx1.SaveChangesAsync();

        entry.Complete(responseBody, resourceId);
        await ctx1.SaveChangesAsync();

        await using var ctx2 = PostgresContainerFixture.BuildFraudContext(_cs);
        var loaded = await ctx2.Set<IdempotencyEntry>()
            .FirstOrDefaultAsync(e => e.Key == key);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(IdempotencyStatus.Completed);
        loaded.ResponseBody.Should().Be(responseBody);
        loaded.ResourceId.Should().Be(resourceId);
        loaded.BodyHash.Should().Be(hash);
    }

    [Test]
    public async Task Rolled_back_insert_leaves_no_entry_in_db()
    {
        var key = $"idem-rollback-{Guid.NewGuid()}";

        await using var ctx = PostgresContainerFixture.BuildFraudContext(_cs);
        ctx.Set<IdempotencyEntry>().Add(IdempotencyEntry.Create(key, "hash", DateTime.UtcNow));

        await using var tx = await ctx.Database.BeginTransactionAsync();
        await ctx.SaveChangesAsync();
        await tx.RollbackAsync();

        await using var ctx2 = PostgresContainerFixture.BuildFraudContext(_cs);
        var found = await ctx2.Set<IdempotencyEntry>().FirstOrDefaultAsync(e => e.Key == key);
        found.Should().BeNull("a rolled-back transaction must leave no trace in the database");
    }

    // --- Concurrency tests (TSK-0089) ---

    /// <summary>
    /// Two concurrent inserts with the same idempotency key: one must commit, the other must
    /// receive PostgreSQL 23505 (unique violation) or 55P03 (lock_timeout). Runs 20 times to
    /// confirm the behaviour is deterministic and there are no intermittent 500s.
    /// </summary>
    [Test]
    public async Task Concurrent_inserts_same_key_one_succeeds_other_gets_conflict_or_timeout()
    {
        for (var i = 0; i < 20; i++)
        {
            var key = $"idem-concurrent-{Guid.NewGuid()}";

            var t1 = TryCommitWithLockTimeoutAsync(key, "hash");
            var t2 = TryCommitWithLockTimeoutAsync(key, "hash");
            var results = await Task.WhenAll(t1, t2);

            results.Should().Contain(true,
                $"[iteration {i}] at least one concurrent request must commit successfully");
            results.Should().Contain(false,
                $"[iteration {i}] the other concurrent request must receive a conflict or lock timeout (23505 / 55P03), not an unhandled 500");
        }
    }

    // Simulates the UnitOfWork commit: explicit transaction + lock_timeout + idempotency conflict detection.
    private async Task<bool> TryCommitWithLockTimeoutAsync(string key, string hash)
    {
        await using var ctx = PostgresContainerFixture.BuildFraudContext(_cs);
        ctx.Set<IdempotencyEntry>().Add(IdempotencyEntry.Create(key, hash, DateTime.UtcNow));

        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            await ctx.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '1500ms'");
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
            && pgEx.SqlState is "23505" or "55P03")
        {
            await tx.RollbackAsync(CancellationToken.None);
            return false;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
