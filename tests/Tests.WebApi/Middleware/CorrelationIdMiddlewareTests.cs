using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using BookStore.Api.Infrastructure.Middleware;

namespace Tests.WebApi.Middleware;

[Unit]
public sealed class CorrelationIdMiddlewareTests : UnitTestsBase
{
    private const string HeaderName = "X-Correlation-Id";

    [Test]
    public async Task Request_without_correlation_header_receives_generated_id_in_response()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

        // Assert
        context.Response.Headers.ContainsKey(HeaderName).Should().BeTrue();
        var responseId = context.Response.Headers[HeaderName].ToString();
        responseId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(responseId, out _).Should().BeTrue();
    }

    [Test]
    public async Task Request_with_correlation_header_echoes_same_value_in_response()
    {
        // Arrange
        var knownId = Guid.NewGuid().ToString();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = knownId;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

        // Assert
        context.Response.Headers[HeaderName].ToString().Should().Be(knownId);
    }

    [Test]
    public async Task Request_with_blank_correlation_header_receives_generated_id_in_response()
    {
        // Arrange
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = "   ";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, NullLogger<CorrelationIdMiddleware>.Instance);

        // Assert
        var responseId = context.Response.Headers[HeaderName].ToString();
        responseId.Should().NotBeNullOrWhiteSpace();
        responseId.Should().NotBe("   ");
    }
}
