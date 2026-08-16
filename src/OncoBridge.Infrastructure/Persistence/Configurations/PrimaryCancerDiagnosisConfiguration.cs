using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class PrimaryCancerDiagnosisConfiguration
    : IEntityTypeConfiguration<PrimaryCancerDiagnosis>
{
    public void Configure(EntityTypeBuilder<PrimaryCancerDiagnosis> builder)
    {
        builder.ToTable("primary_cancer_diagnosis");

        builder.HasKey(diagnosis => diagnosis.Id);

        builder.Property(diagnosis => diagnosis.Id)
            .HasColumnName("id")
            .HasConversion(OncoBridgeValueConverters.PrimaryCancerDiagnosisId)
            .ValueGeneratedNever();

        builder.Property(diagnosis => diagnosis.PatientId)
            .HasColumnName("patient_id")
            .HasConversion(OncoBridgeValueConverters.PatientId)
            .IsRequired();

        builder.ComplexProperty(diagnosis => diagnosis.Code, coded => CanonicalColumns.Coded(coded, "code"));

        builder.ComplexProperty(
            diagnosis => diagnosis.BodySite, coded => CanonicalColumns.Coded(coded, "body_site"));

        builder.ComplexProperty(
            diagnosis => diagnosis.Onset, onset => CanonicalColumns.Occurrence(onset, "onset"));

        builder.ComplexProperty(
            diagnosis => diagnosis.RecordedDate, date => CanonicalColumns.Date(date, "recorded_date"));

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(diagnosis => diagnosis.PatientId)
            .HasConstraintName("fk_primary_cancer_diagnosis_patient")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(diagnosis => diagnosis.PatientId)
            .HasDatabaseName("ix_primary_cancer_diagnosis_patient_id");

        CanonicalColumns.OwnedByBatch(
            builder, "fk_primary_cancer_diagnosis_batch", "ix_primary_cancer_diagnosis_batch_id");
    }
}
