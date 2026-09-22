using Domain.Entities.Catalog;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.BookStore.Repositories;

internal sealed class BookRepository(BookStoreDbContext context) : IBookRepository
{
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Books.FindAsync([id], cancellationToken);

    public void Add(Book entity) => context.Books.Add(entity);

    public async Task<IReadOnlyList<Book>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await context.Books.OrderBy(b => b.Title).ToListAsync(cancellationToken);
}
