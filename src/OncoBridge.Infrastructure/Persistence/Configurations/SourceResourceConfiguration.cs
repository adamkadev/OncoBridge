using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class SourceResourceConfiguration : IEntityTypeConfiguration<SourceResource>
{
    public void Configure(EntityTypeBuilder<SourceResource> builder)
    {
        builder.ToTable("source_resource");

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id)
            .HasColumnName("id")
            .HasConversion(OncoBridgeValueConverters.SourceResourceId)
            .ValueGeneratedNever();

        builder.Property(resource => resource.BatchId)
            .HasColumnName("batch_id")
            .HasConversion(OncoBridgeValueConverters.ImportBatchId)
            .IsRequired();

        builder.Property(resource => resource.EntryIndex)
            .HasColumnName("entry_index")
            .IsRequired();

        builder.Property(resource => resource.ResourceType)
            .HasColumnName("resource_type")
            .HasMaxLength(100);

        builder.Property(resource => resource.ContentHash)
            .HasColumnName("content_hash")
            .HasConversion(OncoBridgeValueConverters.ContentHash)
            .HasMaxLength(64);

        builder.Property(resource => resource.ResourceJson)
            .HasColumnName("resource_json")
            .HasColumnType("jsonb");

        builder.Property(resource => resource.SourceLogicalId)
            .HasColumnName("source_logical_id")
            .HasMaxLength(200);

        builder.Property(resource => resource.FullUrl)
            .HasColumnName("full_url")
            .HasMaxLength(2000);

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(resource => resource.BatchId)
            .HasConstraintName("fk_source_resource_batch")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(resource => new { resource.BatchId, resource.EntryIndex })
            .IsUnique()
            .HasDatabaseName("ux_source_resource_batch_entry_index");

        builder.HasIndex(resource => resource.ResourceType)
            .HasDatabaseName("ix_source_resource_resource_type");
    }
}
