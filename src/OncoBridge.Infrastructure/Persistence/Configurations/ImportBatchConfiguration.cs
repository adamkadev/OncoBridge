using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batch");

        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Id)
            .HasColumnName("id")
            .HasConversion(OncoBridgeValueConverters.ImportBatchId)
            .ValueGeneratedNever();

        builder.Property(batch => batch.SourceSystemLabel)
            .HasColumnName("source_system_label")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(batch => batch.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamptz")
            .HasConversion(OncoBridgeValueConverters.UtcInstant)
            .IsRequired();

        builder.Property(batch => batch.RawPayload)
            .HasColumnName("raw_payload")
            .HasColumnType("bytea")
            .HasConversion(OncoBridgeValueConverters.RawPayload, OncoBridgeValueConverters.RawPayloadComparer)
            .IsRequired();

        builder.Property(batch => batch.ContentHash)
            .HasColumnName("content_hash")
            .HasConversion(OncoBridgeValueConverters.ContentHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(batch => batch.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500);

        builder.Property(batch => batch.BundleType)
            .HasColumnName("bundle_type")
            .HasMaxLength(100);

        builder.Property(batch => batch.EntryCount)
            .HasColumnName("entry_count")
            .IsRequired();

        builder.Property(batch => batch.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(batch => batch.NormalizerVersion)
            .HasColumnName("normalizer_version")
            .HasMaxLength(100);

        builder.HasIndex(batch => batch.ContentHash).HasDatabaseName("ix_import_batch_content_hash");
    }
}
