using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class LineageConfiguration : IEntityTypeConfiguration<Lineage>
{
    public void Configure(EntityTypeBuilder<Lineage> builder)
    {
        builder.ToTable("lineage");

        builder.Property<Guid>("Id").HasColumnName("id").ValueGeneratedOnAdd();
        builder.HasKey("Id");

        builder.Property(lineage => lineage.DomainEntityType)
            .HasColumnName("domain_entity_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lineage => lineage.DomainEntityId)
            .HasColumnName("domain_entity_id")
            .IsRequired();

        builder.Property(lineage => lineage.FieldPath)
            .HasColumnName("field_path")
            .HasMaxLength(500);

        builder.Property(lineage => lineage.SourceResourceId)
            .HasColumnName("source_resource_id")
            .HasConversion(OncoBridgeValueConverters.SourceResourceId)
            .IsRequired();

        builder.Property(lineage => lineage.TransformationName)
            .HasColumnName("transformation_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lineage => lineage.TransformationVersion)
            .HasColumnName("transformation_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<SourceResource>()
            .WithMany()
            .HasForeignKey(lineage => lineage.SourceResourceId)
            .HasConstraintName("fk_lineage_source_resource")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(lineage => new { lineage.DomainEntityType, lineage.DomainEntityId })
            .HasDatabaseName("ix_lineage_domain_entity");
    }
}
