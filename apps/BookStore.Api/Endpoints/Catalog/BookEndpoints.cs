using Application.UseCases.Catalog.ListBooks;
using BookStore.Api.Infrastructure.Extensions;
using Cortex.Mediator;

namespace BookStore.Api.Endpoints.Catalog;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGroup("/books").WithTags("Books")
            .MapGet("/", ListAsync)
            .Produces<ListBooksResponse>(200)
            .WithName("ListBooks");

        return api;
    }

    private static async Task<IResult> ListAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendQueryAsync(new ListBooksRequest(), cancellationToken);
        return result.ToHttpResult();
    }
}
