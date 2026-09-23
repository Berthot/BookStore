using System.Text;
using Application.Abstractions.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using WebApi.Filters.Idempotency;

namespace Tests.WebApi.Filters;

[Unit]
public sealed class IdempotencyFilterTests : UnitTestsBase
{
    private readonly IIdempotencyStore _store = Substitute.For<IIdempotencyStore>();
    private readonly IdempotencyFilter<IIdempotencyStore> _filter;

    public IdempotencyFilterTests()
    {
        _filter = new IdempotencyFilter<IIdempotencyStore>(_store);
    }

    private static DefaultHttpContext BuildHttpContext(string? key = null, string body = "{}")
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        if (key is not null)
            context.Request.Headers["Idempotency-Key"] = key;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/json";
        return context;
    }

    private static async Task<int> GetStatusCodeAsync(object? result, DefaultHttpContext httpContext)
    {
        if (result is IResult httpResult)
            await httpResult.ExecuteAsync(httpContext);
        return httpContext.Response.StatusCode;
    }

    [Test]
    public async Task InvokeAsync_absent_header_returns_400_without_touching_store()
    {
        var httpContext = BuildHttpContext();
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>(null));

        var statusCode = await GetStatusCodeAsync(result, httpContext);
        statusCode.Should().Be(400);
        await _store.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _store.DidNotReceive().Add(Arg.Any<IdempotencyEntry>());
    }

    [Test]
    public async Task InvokeAsync_invalid_json_body_returns_400_without_registering_key()
    {
        var httpContext = BuildHttpContext("idempotency-key-1", "not-valid-json");
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>(null));

        var statusCode = await GetStatusCodeAsync(result, httpContext);
        statusCode.Should().Be(400);
        _store.DidNotReceive().Add(Arg.Any<IdempotencyEntry>());
    }

    [Test]
    public async Task InvokeAsync_new_key_adds_entry_stores_it_in_items_and_calls_next()
    {
        const string key = "new-idempotency-key";
        _store.FindAsync(key, Arg.Any<CancellationToken>()).Returns((IdempotencyEntry?)null);
        var httpContext = BuildHttpContext(key, """{"amount":100}""");
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;

        await _filter.InvokeAsync(filterContext, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Accepted(""));
        });

        nextCalled.Should().BeTrue();
        _store.Received(1).Add(Arg.Is<IdempotencyEntry>(e => e.Key == key));
        httpContext.Items[IdempotencyFilter<IIdempotencyStore>.HttpContextEntryKey]
            .Should().BeOfType<IdempotencyEntry>();
    }

    [Test]
    public async Task InvokeAsync_completed_same_hash_replays_stored_body_without_calling_next()
    {
        const string key = "replay-key";
        const string body = """{"amount":100}""";
        var hash = IdempotencyHasher.ComputeHash(body);
        const string storedResponse = """{"id":"abc","status":"processing"}""";

        var entry = IdempotencyEntry.Create(key, hash, DateTime.UtcNow);
        entry.Complete(storedResponse, Guid.NewGuid());
        _store.FindAsync(key, Arg.Any<CancellationToken>()).Returns(entry);

        var httpContext = BuildHttpContext(key, body);
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);
        var nextCalled = false;

        var result = await _filter.InvokeAsync(filterContext, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(null);
        });

        nextCalled.Should().BeFalse();
        var statusCode = await GetStatusCodeAsync(result, httpContext);
        statusCode.Should().Be(200);
    }

    [Test]
    public async Task InvokeAsync_completed_different_hash_returns_422()
    {
        const string key = "hash-diff-key";
        var originalHash = IdempotencyHasher.ComputeHash("""{"amount":100}""");
        var entry = IdempotencyEntry.Create(key, originalHash, DateTime.UtcNow);
        entry.Complete("""{"id":"abc"}""", null);

        _store.FindAsync(key, Arg.Any<CancellationToken>()).Returns(entry);
        var httpContext = BuildHttpContext(key, """{"amount":200}""");
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>(null));

        var statusCode = await GetStatusCodeAsync(result, httpContext);
        statusCode.Should().Be(422);
    }

    [Test]
    public async Task InvokeAsync_processing_entry_returns_409()
    {
        const string key = "in-flight-key";
        var hash = IdempotencyHasher.ComputeHash("{}");
        var entry = IdempotencyEntry.Create(key, hash, DateTime.UtcNow);
        // Entry stays in Processing (default) — not completed yet.

        _store.FindAsync(key, Arg.Any<CancellationToken>()).Returns(entry);
        var httpContext = BuildHttpContext(key);
        var filterContext = EndpointFilterInvocationContext.Create(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>(null));

        var statusCode = await GetStatusCodeAsync(result, httpContext);
        statusCode.Should().Be(409);
    }
}
