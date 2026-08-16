using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class StageCategoryConfiguration : IEntityTypeConfiguration<StageCategory>
{
    internal const string StagingIdProperty = "StagingId";

    private const string KeyProperty = "Id";

    public void Configure(EntityTypeBuilder<StageCategory> builder)
    {
        builder.ToTable("stage_category");

        builder.Property<Guid>(KeyProperty).HasColumnName("id").ValueGeneratedOnAdd();
        builder.HasKey(KeyProperty);

        builder.Property<Guid>(StagingIdProperty).HasColumnName("staging_id").IsRequired();

        builder.Property(category => category.Axis)
            .HasColumnName("axis")
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(category => category.SourceResourceId)
            .HasColumnName("source_resource_id")
            .HasConversion(OncoBridgeValueConverters.SourceResourceId)
            .IsRequired();

        builder.ComplexProperty(category => category.Code, coded => CanonicalColumns.Coded(coded, "code"));

        builder.HasOne<SourceResource>()
            .WithMany()
            .HasForeignKey(category => category.SourceResourceId)
            .HasConstraintName("fk_stage_category_source_resource")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(category => category.SourceResourceId)
            .HasDatabaseName("ix_stage_category_source_resource_id");

        builder.HasIndex(StagingIdProperty, nameof(StageCategory.Axis))
            .IsUnique()
            .HasDatabaseName("ux_stage_category_staging_axis");
    }
}
