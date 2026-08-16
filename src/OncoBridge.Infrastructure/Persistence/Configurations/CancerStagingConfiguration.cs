using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class CancerStagingConfiguration : IEntityTypeConfiguration<CancerStaging>
{
    public void Configure(EntityTypeBuilder<CancerStaging> builder)
    {
        builder.ToTable("cancer_staging");

        builder.HasKey(staging => staging.Id);

        builder.Property(staging => staging.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(staging => staging.PatientId)
            .HasColumnName("patient_id")
            .HasConversion(OncoBridgeValueConverters.PatientId)
            .IsRequired();

        builder.Property(staging => staging.PrimaryCancerDiagnosisId)
            .HasColumnName("primary_cancer_diagnosis_id")
            .HasConversion(OncoBridgeValueConverters.PrimaryCancerDiagnosisId)
            .IsRequired();

        builder.ComplexProperty(
            staging => staging.StageGroup, coded => CanonicalColumns.Coded(coded, "stage_group"));

        builder.ComplexProperty(
            staging => staging.Method,
            method =>
            {
                CanonicalColumns.Present(method, "method_kind");
                method.ComplexProperty(
                    value => value.Code, coded => CanonicalColumns.Coded(coded, "method"));
            });

        builder.ComplexProperty(
            staging => staging.Effective, date => CanonicalColumns.Date(date, "effective"));

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(staging => staging.PatientId)
            .HasConstraintName("fk_cancer_staging_patient")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<PrimaryCancerDiagnosis>()
            .WithMany()
            .HasForeignKey(staging => staging.PrimaryCancerDiagnosisId)
            .HasConstraintName("fk_cancer_staging_primary_cancer_diagnosis")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(staging => staging.Categories)
            .WithOne()
            .HasForeignKey(StageCategoryConfiguration.StagingIdProperty)
            .HasConstraintName("fk_stage_category_staging")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(staging => staging.Categories).AutoInclude();

        builder.HasIndex(staging => staging.PatientId)
            .HasDatabaseName("ix_cancer_staging_patient_id");

        builder.HasIndex(staging => staging.PrimaryCancerDiagnosisId)
            .HasDatabaseName("ix_cancer_staging_primary_cancer_diagnosis_id");

        CanonicalColumns.OwnedByBatch(builder, "fk_cancer_staging_batch", "ix_cancer_staging_batch_id");
    }
}
