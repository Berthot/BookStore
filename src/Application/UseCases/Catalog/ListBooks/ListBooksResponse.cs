namespace Application.UseCases.Catalog.ListBooks;

public sealed record ListBooksResponse(IReadOnlyList<BookItemDto> Books);

public sealed record BookItemDto(Guid Id, string Title, string Author, BookPriceDto Price, string Format);

public sealed record BookPriceDto(decimal Value, string Currency);
