using Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.BookStore.Configurations.Catalog;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", "bookstore");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).HasMaxLength(300).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Format).IsRequired();

        builder.OwnsOne(b => b.Price, price =>
        {
            price.Property(p => p.Value).HasColumnName("price_value").HasColumnType("numeric(18,2)").IsRequired();
            price.Property(p => p.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        });
    }
}
