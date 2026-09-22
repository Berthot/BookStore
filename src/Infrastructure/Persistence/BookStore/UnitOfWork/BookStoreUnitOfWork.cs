using Domain.Repositories;

namespace Infrastructure.Persistence.BookStore.UnitOfWork;

internal sealed class BookStoreUnitOfWork(BookStoreDbContext context) : IBookStoreUnitOfWork
{
    /// <summary>Persists all pending changes in the BookStore context. Returns true if at least one row was affected.</summary>
    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken) > 0;
}
