using Application.Commons;
using Application.UseCases.Catalog.ListBooks;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.Catalog;

namespace Tests.Application.UseCases.Catalog.ListBooks;

[Unit]
public sealed class ListBooksHandlerTests : UnitTestsBase
{
    private IBookRepository _repo = null!;
    private ListBooksHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IBookRepository>();
        _handler = new ListBooksHandler(_repo);
    }

    [Test]
    public async Task Handle_returns_all_books_from_repository()
    {
        var books = new[] { BookMother.Simple(), BookMother.Ebook() };
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(books);

        var result = await _handler.Handle(new ListBooksRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Books.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_maps_book_fields_correctly()
    {
        var book = BookMother.Simple();
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([book]);

        var result = await _handler.Handle(new ListBooksRequest(), CancellationToken.None);

        var dto = result.Data!.Books.Single();
        dto.Id.Should().Be(book.Id);
        dto.Title.Should().Be(book.Title);
        dto.Author.Should().Be(book.Author);
        dto.Price.Value.Should().Be(book.Price.Value);
        dto.Price.Currency.Should().Be(book.Price.Currency);
        dto.Format.Should().Be(book.Format.ToString().ToUpperInvariant());
    }

    [Test]
    public async Task Handle_returns_empty_list_when_no_books()
    {
        _repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Domain.Entities.Catalog.Book>());

        var result = await _handler.Handle(new ListBooksRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Books.Should().BeEmpty();
    }
}
