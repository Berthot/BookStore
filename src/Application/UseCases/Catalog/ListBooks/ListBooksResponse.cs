using Domain.Enums;

namespace Application.UseCases.Catalog.ListBooks;

public sealed record ListBooksResponse(IReadOnlyList<BookItemDto> Books);

public sealed record BookItemDto(Guid Id, string Title, string Author, BookPriceDto Price, BookFormat Format);

public sealed record BookPriceDto(decimal Value, string Currency);
