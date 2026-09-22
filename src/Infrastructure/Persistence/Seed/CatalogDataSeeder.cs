using Domain.Entities.Catalog;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.BookStore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed;

/// <summary>Seeds the Books catalogue with representative data covering all fraud test scenarios (physical cheap, e-book expensive, physical mid-price).</summary>
internal sealed class CatalogDataSeeder(BookStoreDbContext context) : ICatalogSeeder
{
    private static readonly Book[] SeedBooks =
    [
        new()
        {
            Id = new Guid("11111111-0000-0000-0000-000000000001"),
            Title = "Clean Code",
            Author = "Robert C. Martin",
            Price = new Money(49.90m, "BRL"),
            Format = BookFormat.Paperback,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = new Guid("11111111-0000-0000-0000-000000000002"),
            Title = "The Pragmatic Programmer",
            Author = "Andrew Hunt & David Thomas",
            Price = new Money(349.90m, "BRL"),
            Format = BookFormat.Ebook,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = new Guid("11111111-0000-0000-0000-000000000003"),
            Title = "Domain-Driven Design",
            Author = "Eric Evans",
            Price = new Money(149.90m, "BRL"),
            Format = BookFormat.Hardcover,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Books.AnyAsync(cancellationToken))
            return;

        context.Books.AddRange(SeedBooks);
        await context.SaveChangesAsync(cancellationToken);
    }
}
