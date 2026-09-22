using Application.Abstractions.Idempotency;
using Domain.Entities.Catalog;
using Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence.BookStore;

public sealed class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<IdempotencyEntry> IdempotencyKeys => Set<IdempotencyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bookstore");
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith("Infrastructure.Persistence.BookStore.Configurations") == true);
    }
}
