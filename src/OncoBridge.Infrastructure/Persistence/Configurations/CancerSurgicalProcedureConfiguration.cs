using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Infrastructure.Persistence.Configurations;

internal sealed class CancerSurgicalProcedureConfiguration
    : IEntityTypeConfiguration<CancerSurgicalProcedure>
{
    public void Configure(EntityTypeBuilder<CancerSurgicalProcedure> builder)
    {
        builder.ToTable("cancer_surgical_procedure");

        builder.HasKey(procedure => procedure.Id);

        builder.Property(procedure => procedure.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(procedure => procedure.PatientId)
            .HasColumnName("patient_id")
            .HasConversion(OncoBridgeValueConverters.PatientId)
            .IsRequired();

        builder.ComplexProperty(procedure => procedure.Code, coded => CanonicalColumns.Coded(coded, "code"));

        builder.ComplexProperty(
            procedure => procedure.BodySite, coded => CanonicalColumns.Coded(coded, "body_site"));

        builder.ComplexProperty(
            procedure => procedure.Performed,
            performed => CanonicalColumns.Occurrence(performed, "performed"));

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(procedure => procedure.PatientId)
            .HasConstraintName("fk_cancer_surgical_procedure_patient")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(procedure => procedure.PatientId)
            .HasDatabaseName("ix_cancer_surgical_procedure_patient_id");

        CanonicalColumns.OwnedByBatch(
            builder, "fk_cancer_surgical_procedure_batch", "ix_cancer_surgical_procedure_batch_id");
    }
}
