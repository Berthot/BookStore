using Domain.Entities.Sales;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.BookStore.Repositories;

internal sealed class PurchaseRepository(BookStoreDbContext context) : IPurchaseRepository
{
    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Purchases.FindAsync([id], cancellationToken);

    public void Add(Purchase entity) => context.Purchases.Add(entity);

    public async Task<Purchase?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await context.Purchases.FirstOrDefaultAsync(p => p.CorrelationId == correlationId, cancellationToken);
}
