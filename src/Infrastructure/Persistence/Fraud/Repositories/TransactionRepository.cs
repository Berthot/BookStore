using Domain.Entities.FraudAnalysis;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Fraud.Repositories;

internal sealed class TransactionRepository(FraudDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Assessments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Transaction entity) => context.Transactions.Add(entity);

    public async Task<Transaction?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Assessments)
            .FirstOrDefaultAsync(t => t.CorrelationId == correlationId, cancellationToken);
}
