using Application.Abstractions.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Fraud.Configurations.Idempotency;

internal sealed class IdempotencyEntryConfiguration : IEntityTypeConfiguration<IdempotencyEntry>
{
    public void Configure(EntityTypeBuilder<IdempotencyEntry> builder)
    {
        builder.ToTable("idempotency_keys", "fraud");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key).HasMaxLength(IdempotencyEntry.KeyMaxLength).IsRequired();
        builder.Property(e => e.BodyHash).HasMaxLength(IdempotencyEntry.HashMaxLength).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ResponseBody).HasColumnType("text");
        builder.Property(e => e.StatusCode).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.LocationHeader).HasMaxLength(500);

        builder.HasIndex(e => e.Key)
            .IsUnique()
            .HasDatabaseName("ix_fraud_idempotency_keys_key");
    }
}
