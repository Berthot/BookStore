using Domain.Entities.FraudAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Fraud.Configurations.FraudAnalysis;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", "fraud");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalReference).HasMaxLength(200);
        builder.Property(t => t.CustomerId).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Channel).IsRequired();
        builder.Property(t => t.DeliveryType).IsRequired();
        builder.Property(t => t.ItemCount).IsRequired();
        builder.Property(t => t.OccurredAt).IsRequired();
        builder.Property(t => t.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Status).IsRequired();

        builder.OwnsOne(t => t.Amount, amount =>
        {
            amount.Property(a => a.Value).HasColumnName("amount_value").HasColumnType("numeric(18,2)").IsRequired();
            amount.Property(a => a.Currency).HasColumnName("amount_currency").HasMaxLength(3).IsRequired();
        });

        builder.ComplexProperty(t => t.Payment, payment =>
        {
            payment.Property(p => p.Type).HasColumnName("payment_type").HasMaxLength(50).IsRequired();
            // Fingerprint is stored separately on Transaction for indexing; keep a reference copy here
            payment.Property(p => p.Fingerprint).HasColumnName("payment_instrument_fingerprint").HasMaxLength(100).IsRequired();
            payment.Property(p => p.Last4).HasColumnName("payment_last4").HasMaxLength(4);
        });

        // First-class column for composite index (EF Core cannot index complex-type sub-properties directly)
        builder.Property(t => t.PaymentFingerprint)
            .HasColumnName("payment_fingerprint")
            .HasMaxLength(100)
            .IsRequired();

        // Index covering card velocity (CARD_VELOCITY rule) and structuring queries
        builder.HasIndex(t => new { t.PaymentFingerprint, t.CreatedAt })
            .HasDatabaseName("ix_transactions_payment_fingerprint_occurred_at");

        builder.HasMany(t => t.Assessments)
            .WithOne()
            .HasForeignKey(a => a.TransactionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
