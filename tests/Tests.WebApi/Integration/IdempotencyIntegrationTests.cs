using System.Text;
using Application.Abstractions.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using WebApi.Filters.Idempotency;

namespace Tests.WebApi.Integration;

// Simulates the persistent store (shared across request scopes — like the real database)
internal sealed class InMemoryIdempotencyDatabase
{
    public Dictionary<string, IdempotencyEntry> Committed { get; } = new();
}

// Simulates the EF change tracker (one instance per request scope — discarded if not committed)
internal sealed class InMemoryFraudIdempotencyStore(InMemoryIdempotencyDatabase db) : IFraudIdempotencyStore
{
    private readonly List<IdempotencyEntry> _pending = [];

    public Task<IdempotencyEntry?> FindAsync(string key, CancellationToken ct = default)
        => Task.FromResult(db.Committed.GetValueOrDefault(key));

    public void Add(IdempotencyEntry entry) => _pending.Add(entry);

    public Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default)
        => Task.FromResult(0);

    // Mirrors SaveChangesAsync — flushes pending entries into the shared committed store
    public void Commit()
    {
        foreach (var e in _pending)
            db.Committed[e.Key] = e;
        _pending.Clear();
    }
}

/// <summary>
/// Unit tests for IdempotencyFilter semantics (no database).
/// Uses an in-memory store that mirrors the two-phase commit pattern: entry added → handler
/// commits → entry marked complete → second commit. Each "request" creates a new store scope;
/// the singleton database survives across scopes, mirroring PostgreSQL behaviour.
///
/// Real database tests (unique index, lock_timeout, concurrency) live in
/// tests/Tests.Infrastructure/Persistence/IdempotencyIntegrationTests.cs.
/// </summary>
[Unit]
public sealed class IdempotencyFilterTests : UnitTestsBase
{
    private InMemoryIdempotencyDatabase _db = null!;

    [SetUp]
    public void SetUp() => _db = new InMemoryIdempotencyDatabase();

    // Simulates one HTTP request: filter → (optional) success handler → (optional) commit
    private async Task<int> ExecuteRequestAsync(
        string idempotencyKey,
        string body,
        bool handlerSucceeds)
    {
        // New store per call = new request scope
        var store = new InMemoryFraudIdempotencyStore(_db);
        var filter = new IdempotencyFilter<IFraudIdempotencyStore>(store);

        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var ctx = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        ctx.Request.Headers["Idempotency-Key"] = idempotencyKey;
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        ctx.Request.ContentType = "application/json";

        var filterContext = EndpointFilterInvocationContext.Create(ctx);

        var result = await filter.InvokeAsync(filterContext, async _ =>
        {
            if (!handlerSucceeds)
                return Results.Problem(statusCode: 500, detail: "handler failure");

            // Phase 1: handler commits transaction entity + idempotency entry together
            store.Commit();

            // Phase 2: endpoint completes the entry and commits the status update
            if (ctx.Items[IdempotencyFilter<IFraudIdempotencyStore>.HttpContextEntryKey]
                is IdempotencyEntry entry)
            {
                const string responseJson =
                    """{"transactionId":"aaaaaaaa-0000-0000-0000-000000000001","status":"RECEIVED"}""";
                entry.Complete(responseJson, Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
                store.Commit(); // object mutation is visible via the db reference — no-op for pending
            }

            return Results.Created("/api/v1/transactions/aaaaaaaa-0000-0000-0000-000000000001",
                new { transactionId = "aaaaaaaa-0000-0000-0000-000000000001", status = "RECEIVED" });
        });

        if (result is IResult httpResult)
            await httpResult.ExecuteAsync(ctx);

        return ctx.Response.StatusCode;
    }

    [Test]
    public async Task Same_key_same_body_second_request_replays_stored_response_with_200()
    {
        const string key = "idem-replay-001";
        const string body = """{"amount":100,"currency":"BRL"}""";

        var status1 = await ExecuteRequestAsync(key, body, handlerSucceeds: true);
        status1.Should().Be(201, "first request creates the resource");

        var status2 = await ExecuteRequestAsync(key, body, handlerSucceeds: true);
        status2.Should().Be(200,
            "replay returns 200 with the stored response, no new resource created");
    }

    [Test]
    public async Task Same_key_different_body_returns_422()
    {
        const string key = "idem-conflict-002";

        await ExecuteRequestAsync(key, """{"amount":100}""", handlerSucceeds: true);
        var status2 = await ExecuteRequestAsync(key, """{"amount":999}""", handlerSucceeds: true);

        status2.Should().Be(422,
            "a different body on an already-completed key must return 422 Unprocessable Entity");
    }

    [Test]
    public async Task Handler_failure_does_not_commit_entry_so_same_key_is_retryable()
    {
        const string key = "idem-retry-003";
        const string body = """{"amount":100}""";

        // Handler failure — store.Commit() is never called; the pending entry is discarded with the scope
        var status1 = await ExecuteRequestAsync(key, body, handlerSucceeds: false);
        status1.Should().Be(500, "first request fails at the handler");

        // The key is not in the committed store — the retry with same key must succeed
        var status2 = await ExecuteRequestAsync(key, body, handlerSucceeds: true);
        status2.Should().Be(201,
            "the key is retryable because the failed entry was never committed");
    }

    // --- TSK-0089: Concurrency ---

    private DefaultHttpContext BuildCtx(string idempotencyKey, string body)
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var ctx = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        ctx.Request.Headers["Idempotency-Key"] = idempotencyKey;
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        ctx.Request.ContentType = "application/json";
        return ctx;
    }

    [Test]
    public async Task Concurrent_requests_same_key_one_proceeds_other_gets_409_while_first_is_processing()
    {
        const string key = "idem-concurrent-004";
        const string body = """{"amount":100}""";

        // Gates to synchronise the two concurrent requests
        var firstHandlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstHandlerCanComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Request 1: processes the key and holds in "Processing" state
        var store1 = new InMemoryFraudIdempotencyStore(_db);
        var filter1 = new IdempotencyFilter<IFraudIdempotencyStore>(store1);
        var ctx1 = BuildCtx(key, body);

        var request1 = filter1.InvokeAsync(
            EndpointFilterInvocationContext.Create(ctx1),
            async _ =>
            {
                // Phase 1: commit entry as Processing so concurrent readers see it
                store1.Commit();

                firstHandlerStarted.SetResult(); // tell Request 2 it can start
                await firstHandlerCanComplete.Task; // wait for Request 2 to finish

                // Phase 2: complete and commit the status update
                if (ctx1.Items[IdempotencyFilter<IFraudIdempotencyStore>.HttpContextEntryKey]
                    is IdempotencyEntry entry)
                {
                    const string responseJson =
                        """{"transactionId":"bbbbbbbb-0000-0000-0000-000000000002","status":"RECEIVED"}""";
                    entry.Complete(responseJson, Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"));
                    // No second commit needed: object reference in db is already mutated.
                }

                return Results.Created("/api/v1/transactions/bbbbbbbb-0000-0000-0000-000000000002",
                    new { });
            });

        // Wait until Request 1 has committed the "Processing" entry
        await firstHandlerStarted.Task;

        // Request 2: arrives while Request 1 is in-flight — must see the Processing entry and get 409
        var store2 = new InMemoryFraudIdempotencyStore(_db);
        var filter2 = new IdempotencyFilter<IFraudIdempotencyStore>(store2);
        var ctx2 = BuildCtx(key, body);
        var filterContext2 = EndpointFilterInvocationContext.Create(ctx2);

        var result2 = await filter2.InvokeAsync(filterContext2,
            _ => ValueTask.FromResult<object?>(Results.Created("/ignored", new { })));

        if (result2 is IResult httpResult2)
            await httpResult2.ExecuteAsync(ctx2);

        ctx2.Response.StatusCode.Should().Be(409,
            "a concurrent request with the same key must receive 409 Conflict while the first is still processing");

        // Let Request 1 finish
        firstHandlerCanComplete.SetResult();
        var finalResult = await request1;
        if (finalResult is IResult httpResult1)
            await httpResult1.ExecuteAsync(ctx1);

        ctx1.Response.StatusCode.Should().Be(201, "the original request must complete successfully");
    }

    [Test]
    public async Task Concurrent_requests_same_key_second_arriving_after_completion_gets_replay()
    {
        const string key = "idem-concurrent-005";
        const string body = """{"amount":100}""";

        // Request 1 completes fully
        var status1 = await ExecuteRequestAsync(key, body, handlerSucceeds: true);
        status1.Should().Be(201);

        // Request 2 arrives after Request 1 is done — sees a Completed entry with same hash → replay (200)
        var status2 = await ExecuteRequestAsync(key, body, handlerSucceeds: true);
        status2.Should().Be(200, "request arriving after completion with the same body gets a replay (200)");
    }
}
