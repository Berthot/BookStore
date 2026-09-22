namespace Application.Abstractions.Idempotency;

/// <summary>Provides idempotency key storage. Implementations attach the entry to the caller's DbContext — SaveChanges is the caller's responsibility.</summary>
public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> FindAsync(string key, CancellationToken cancellationToken = default);
    void Add(IdempotencyEntry entry);
}

/// <summary>Idempotency store scoped to the BookStore schema.</summary>
public interface IBookStoreIdempotencyStore : IIdempotencyStore { }

/// <summary>Idempotency store scoped to the Fraud schema.</summary>
public interface IFraudIdempotencyStore : IIdempotencyStore { }
