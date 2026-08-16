using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    private const string KeyProperty = "Id";

    private const string TargetShapeConstraint = "ck_finding_target_shape";

    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable(
            "finding",
            table => table.HasCheckConstraint(
                TargetShapeConstraint,
                """
                (target_kind = 'SourceResource' AND target_domain_entity_type IS NULL)
                OR (target_kind = 'DomainEntity' AND target_domain_entity_type IS NOT NULL)
                """));

        builder.Property<Guid>(KeyProperty).HasColumnName("id").ValueGeneratedOnAdd();
        builder.HasKey(KeyProperty);

        builder.Property(finding => finding.CheckId)
            .HasColumnName("check_id")
            .HasConversion(OncoBridgeValueConverters.CheckId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(finding => finding.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(finding => finding.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(finding => finding.Message)
            .HasColumnName("message")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(finding => finding.Citation)
            .HasColumnName("citation")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(finding => finding.Expected).HasColumnName("expected").HasMaxLength(2000);

        builder.Property(finding => finding.Actual).HasColumnName("actual").HasMaxLength(2000);

        builder.ComplexProperty(
            finding => finding.Target,
            target =>
            {
                target.Property(value => value.Kind)
                    .HasColumnName("target_kind")
                    .HasConversion<string>()
                    .HasMaxLength(50);

                target.Property(value => value.Id).HasColumnName("target_id");

                target.Property(value => value.DomainEntityType)
                    .HasColumnName("target_domain_entity_type")
                    .HasMaxLength(200);
            });

        CanonicalColumns.OwnedByBatch(builder, "fk_finding_batch", "ix_finding_batch_id");

        builder.HasIndex(CanonicalColumns.BatchIdProperty, nameof(Finding.Category))
            .HasDatabaseName("ix_finding_batch_category");

        builder.HasIndex(CanonicalColumns.BatchIdProperty, nameof(Finding.CheckId))
            .HasDatabaseName("ix_finding_batch_check_id");
    }
}
