using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patient");

        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.Id)
            .HasColumnName("id")
            .HasConversion(OncoBridgeValueConverters.PatientId)
            .ValueGeneratedNever();

        builder.Property(patient => patient.SourceIdentifier)
            .HasColumnName("source_identifier")
            .HasMaxLength(200);

        builder.ComplexProperty(
            patient => patient.BirthDate, date => CanonicalColumns.Date(date, "birth_date"));

        builder.ComplexProperty(
            patient => patient.SexAtBirthAsRecorded,
            coded => CanonicalColumns.Coded(coded, "sex_at_birth"));

        CanonicalColumns.OwnedByBatch(builder, "fk_patient_batch", "ix_patient_batch_id");
    }
}
