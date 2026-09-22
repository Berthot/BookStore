using Domain.Entities.Catalog;
using Domain.Enums;
using Domain.ValueObjects;
using Tests.Shared.Constants;

namespace Tests.Shared.Mothers.Catalog;

public static class BookMother
{
    public static Book Simple() => new BookBuilder().Build();
    public static Book Ebook() => new BookBuilder().WithFormat(BookFormat.Ebook).Build();
    public static Book Expensive(decimal price = 500m) => new BookBuilder().WithPrice(price).Build();
}

public sealed class BookBuilder
{
    private Guid _id = TestConstants.Ids.BookId;
    private string _title = "Clean Architecture";
    private string _author = "Robert C. Martin";
    private decimal _price = 29.99m;
    private string _currency = "BRL";
    private BookFormat _format = BookFormat.Paperback;

    public BookBuilder WithId(Guid id) { _id = id; return this; }
    public BookBuilder WithTitle(string title) { _title = title; return this; }
    public BookBuilder WithFormat(BookFormat format) { _format = format; return this; }
    public BookBuilder WithPrice(decimal price) { _price = price; return this; }

    public Book Build() => new()
    {
        Id = _id,
        Title = _title,
        Author = _author,
        Price = new Money(_price, _currency),
        Format = _format,
        CreatedAt = TestConstants.Dates.FixedUtcNow
    };
}
