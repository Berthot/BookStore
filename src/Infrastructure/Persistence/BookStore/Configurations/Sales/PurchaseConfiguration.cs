using Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.BookStore.Configurations.Sales;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", "bookstore");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.BookId).IsRequired();
        builder.Property(p => p.BookFormat).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Quantity).IsRequired();
        builder.Property(p => p.CustomerId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PaymentType).HasMaxLength(50).IsRequired();
        builder.Property(p => p.PaymentFingerprint).HasMaxLength(255).IsRequired();
        builder.Property(p => p.PaymentLast4).HasMaxLength(4);
        builder.Property(p => p.TransactionId);
        builder.Property(p => p.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Status).IsRequired();

        builder.OwnsOne(p => p.Total, total =>
        {
            total.Property(t => t.Value).HasColumnName("total_value").HasColumnType("numeric(18,2)").IsRequired();
            total.Property(t => t.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(p => p.CorrelationId).IsUnique();
    }
}
