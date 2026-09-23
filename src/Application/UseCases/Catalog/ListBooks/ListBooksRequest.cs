using Application.Commons;
using Cortex.Mediator.Queries;

namespace Application.UseCases.Catalog.ListBooks;

public sealed record ListBooksRequest : IQuery<OperationResult<ListBooksResponse>>;
