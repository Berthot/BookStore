using Application.Abstractions.Idempotency;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Idempotency;

/// <summary>Base idempotency store. Attaches entries to the injected context; never calls SaveChanges.</summary>
internal abstract class IdempotencyStore<TContext>(TContext context) : IIdempotencyStore
    where TContext : DbContext
{
    public Task<IdempotencyEntry?> FindAsync(string key, CancellationToken cancellationToken = default) =>
        context.Set<IdempotencyEntry>().FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

    public void Add(IdempotencyEntry entry) =>
        context.Set<IdempotencyEntry>().Add(entry);

    public Task<int> DeleteExpiredAsync(DateTime before, CancellationToken cancellationToken = default) =>
        context.Set<IdempotencyEntry>().Where(e => e.CreatedAt < before).ExecuteDeleteAsync(cancellationToken);
}

internal sealed class BookStoreIdempotencyStore(BookStoreDbContext context)
    : IdempotencyStore<BookStoreDbContext>(context), IBookStoreIdempotencyStore;

internal sealed class FraudIdempotencyStore(FraudDbContext context)
    : IdempotencyStore<FraudDbContext>(context), IFraudIdempotencyStore;
