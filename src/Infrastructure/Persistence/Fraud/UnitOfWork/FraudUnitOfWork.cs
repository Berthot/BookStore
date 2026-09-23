using Application.Abstractions.Idempotency;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Fraud.UnitOfWork;

internal sealed class FraudUnitOfWork(FraudDbContext context) : IFraudUnitOfWork
{
    /// <summary>
    /// Persists all pending changes inside an explicit transaction.
    /// Sets lock_timeout before each statement so a concurrent request holding the same
    /// idempotency key receives 409 (55P03) or a replay signal (23505) instead of an unhandled 500.
    /// </summary>
    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // ADR-0004: limits wait time for a row lock held by a concurrent request.
            await context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '1500ms'", cancellationToken);
            var rows = await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return rows > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw new IdempotencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "55P03" })
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw new IdempotencyLockTimeoutException(ex);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
