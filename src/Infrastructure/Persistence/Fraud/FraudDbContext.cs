using Application.Abstractions.Idempotency;
using Domain.Entities.FraudAnalysis;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence.Fraud;

public sealed class FraudDbContext(DbContextOptions<FraudDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<IdempotencyEntry> IdempotencyKeys => Set<IdempotencyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fraud");
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith("Infrastructure.Persistence.Fraud.Configurations") == true);

        // MassTransit outbox/inbox tables — created by EF Core migrations alongside entity tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
