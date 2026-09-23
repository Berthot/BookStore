using Application.UseCases.FraudAnalysis.ReviewTransaction;
using Cortex.Mediator;
using WebApi.Extensions;

namespace WebApi.Endpoints.FraudAnalysis;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/transactions").WithTags("Transactions")
            .MapPost("/{id:guid}/review", ReviewAsync)
            .WithName("ReviewTransaction");

        return app;
    }

    private static async Task<IResult> ReviewAsync(
        Guid id,
        ReviewHttpRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ReviewTransactionRequest(id, body.Outcome, body.Justification, body.ReviewerId);
        var result = await mediator.SendCommandAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed record ReviewHttpRequest(string Outcome, string Justification, string? ReviewerId);
