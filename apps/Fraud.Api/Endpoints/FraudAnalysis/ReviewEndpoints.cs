using Application.UseCases.FraudAnalysis.ReviewTransaction;
using Cortex.Mediator;
using Fraud.Api.Infrastructure.Extensions;
using Fraud.Api.Infrastructure.OpenApi;

namespace Fraud.Api.Endpoints.FraudAnalysis;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGroup("/transactions").WithTags("Transactions")
            .MapPost("/{id:guid}/review", ReviewAsync)
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .WithReviewExamples()
            .WithName("ReviewTransaction");

        return api;
    }

    private static async Task<IResult> ReviewAsync(
        Guid id,
        ReviewHttpRequest body,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ReviewTransactionRequest(id, body.Outcome, body.Justification, null);
        var result = await mediator.SendCommandAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed record ReviewHttpRequest(string Outcome, string Justification);
