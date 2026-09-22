using Domain.Entities.FraudAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Fraud.Configurations.FraudAnalysis;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments", "fraud");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TransactionId).IsRequired();
        builder.Property(a => a.Outcome).IsRequired();
        builder.Property(a => a.Justification).HasMaxLength(1000);

        builder.OwnsOne(a => a.Decider, decider =>
        {
            decider.Property(d => d.Kind).HasColumnName("decider_kind").IsRequired();
            decider.Property(d => d.ReviewerId).HasColumnName("decider_reviewer_id").HasMaxLength(100);
        });

        builder.OwnsMany(a => a.Evaluations, eval =>
        {
            eval.ToTable("RuleEvaluations", "fraud");
            eval.WithOwner().HasForeignKey("AssessmentId");
            eval.Property<Guid>("Id").ValueGeneratedOnAdd();
            eval.HasKey("Id");

            eval.Property(e => e.RuleCode).HasMaxLength(100).IsRequired();
            eval.Property(e => e.RuleVersion).HasMaxLength(20).IsRequired();
            eval.Property(e => e.Hit).IsRequired();
            eval.Property(e => e.Weight).HasColumnType("numeric(5,4)").IsRequired();
            eval.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        });
    }
}
