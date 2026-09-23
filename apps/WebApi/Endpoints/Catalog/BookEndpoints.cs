using Application.UseCases.Catalog.ListBooks;
using Cortex.Mediator;
using WebApi.Extensions;

namespace WebApi.Endpoints.Catalog;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/books").WithTags("Books")
            .MapGet("/", ListAsync)
            .WithName("ListBooks");

        return app;
    }

    private static async Task<IResult> ListAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendQueryAsync(new ListBooksRequest(), cancellationToken);
        return result.ToHttpResult();
    }
}
