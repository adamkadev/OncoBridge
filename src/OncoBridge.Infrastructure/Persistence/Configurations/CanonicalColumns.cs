using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal static class CanonicalColumns
{
    internal const string BatchIdProperty = "BatchId";

    private const string DiscriminatorProperty = "Discriminator";

    private const int SystemLength = 500;

    private const int CodeLength = 200;

    private const int DisplayLength = 1000;

    private const int PrecisionLength = 20;

    private const int StatedInstantLength = 40;

    internal static void OwnedByBatch<TEntity>(
        EntityTypeBuilder<TEntity> builder, string constraintName, string indexName)
        where TEntity : class
    {
        builder.Property<ImportBatchId>(BatchIdProperty)
            .HasColumnName("batch_id")
            .HasConversion(OncoBridgeValueConverters.ImportBatchId)
            .IsRequired();

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(BatchIdProperty)
            .HasConstraintName(constraintName)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(BatchIdProperty).HasDatabaseName(indexName);
    }

    internal static void Present<TComplex>(ComplexPropertyBuilder<TComplex> builder, string columnName)
        where TComplex : notnull
    {
        builder.HasDiscriminator();
        builder.Property<string>(DiscriminatorProperty)
            .HasColumnName(columnName)
            .HasMaxLength(PrecisionLength);
    }

    internal static void Coded(ComplexPropertyBuilder<CodedConcept> builder, string prefix)
    {
        builder.Property(concept => concept.System)
            .HasColumnName($"{prefix}_system")
            .HasMaxLength(SystemLength);

        builder.Property(concept => concept.Code)
            .HasColumnName($"{prefix}_code")
            .HasMaxLength(CodeLength);

        builder.Property(concept => concept.Display)
            .HasColumnName($"{prefix}_display")
            .HasMaxLength(DisplayLength);
    }

    internal static void Date(ComplexPropertyBuilder<PartialDate> builder, string prefix)
    {
        builder.Property(date => date.Precision)
            .HasColumnName($"{prefix}_precision")
            .HasConversion<string>()
            .HasMaxLength(PrecisionLength);

        builder.Property(date => date.Year).HasColumnName($"{prefix}_year");
        builder.Property(date => date.Month).HasColumnName($"{prefix}_month");
        builder.Property(date => date.Day).HasColumnName($"{prefix}_day");

        builder.Property(date => date.Instant)
            .HasColumnName($"{prefix}_instant")
            .HasConversion(OncoBridgeValueConverters.StatedInstant)
            .HasMaxLength(StatedInstantLength);
    }

    internal static void Occurrence(ComplexPropertyBuilder<TemporalOccurrence> builder, string prefix)
    {
        Present(builder, $"{prefix}_kind");

        builder.ComplexProperty(occurrence => occurrence.Date, date => Date(date, $"{prefix}_date"));

        builder.ComplexProperty(
            occurrence => occurrence.Period,
            period =>
            {
                Present(period, $"{prefix}_period_kind");
                period.ComplexProperty(span => span.Start, start => Date(start, $"{prefix}_start"));
                period.ComplexProperty(span => span.End, end => Date(end, $"{prefix}_end"));
            });
    }
}
