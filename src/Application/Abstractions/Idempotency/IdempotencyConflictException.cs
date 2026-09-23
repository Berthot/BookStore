namespace Application.Abstractions.Idempotency;

/// <summary>Thrown when the store detects a concurrent insert of the same idempotency key (PostgreSQL 23505).</summary>
public sealed class IdempotencyConflictException(Exception inner) : Exception("Idempotency key conflict.", inner);

/// <summary>Thrown when lock_timeout fires while waiting for a concurrent transaction holding the same key (PostgreSQL 55P03).</summary>
public sealed class IdempotencyLockTimeoutException(Exception inner) : Exception("Idempotency lock timeout.", inner);
