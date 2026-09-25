using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // IsDevelopment() checks ASPNETCORE_ENVIRONMENT; Aspire injects DOTNET_ENVIRONMENT.
        // Check both so stack traces are visible in any non-Production run.
        var isDev = env.IsDevelopment() || env.IsEnvironment("Development")
            || string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                "Development", StringComparison.OrdinalIgnoreCase);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal",
            Detail = isDev ? exception.ToString() : "An unexpected error occurred."
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
