using Application.Commons;
using Cortex.Mediator.Queries;
using Domain.Repositories;

namespace Application.UseCases.Catalog.ListBooks;

public sealed class ListBooksHandler(IBookRepository repository)
    : IQueryHandler<ListBooksRequest, OperationResult<ListBooksResponse>>
{
    public async Task<OperationResult<ListBooksResponse>> Handle(
        ListBooksRequest query,
        CancellationToken cancellationToken)
    {
        var books = await repository.ListAllAsync(cancellationToken);

        var dtos = books.Select(b => new BookItemDto(
            b.Id,
            b.Title,
            b.Author,
            new BookPriceDto(b.Price.Value, b.Price.Currency),
            b.Format.ToString().ToUpperInvariant())).ToList();

        return OperationResult<ListBooksResponse>.SuccessResult(new ListBooksResponse(dtos));
    }
}
