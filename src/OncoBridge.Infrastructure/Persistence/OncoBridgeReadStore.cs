using Microsoft.EntityFrameworkCore;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Infrastructure.Persistence.Configurations;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class OncoBridgeReadStore(OncoBridgeDbContext context) : IOncoBridgeReadStore
{
    public async Task<ImportDetails?> GetImportAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default)
    {
        ImportSummary? summary = await context.ImportBatches
            .AsNoTracking()
            .Where(batch => batch.Id == batchId)
            .Select(batch => new ImportSummary
            {
                Id = batch.Id,
                SourceSystemLabel = batch.SourceSystemLabel,
                ReceivedAt = batch.ReceivedAt,
                ContentHash = batch.ContentHash,
                FileName = batch.FileName,
                BundleType = batch.BundleType,
                EntryCount = batch.EntryCount,
                Status = batch.Status,
                NormalizerVersion = batch.NormalizerVersion,
                NormalizedAt = batch.NormalizedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (summary is null)
        {
            return null;
        }

        List<SourceResource> sourceResources = await context.SourceResources
            .AsNoTracking()
            .Where(resource => resource.BatchId == batchId)
            .OrderBy(resource => resource.EntryIndex)
            .ToListAsync(cancellationToken);

        List<PatientId> patientIds = await context.Patients
            .AsNoTracking()
            .Where(patient =>
                EF.Property<ImportBatchId>(patient, CanonicalColumns.BatchIdProperty) == batchId)
            .OrderBy(patient => patient.Id)
            .Select(patient => patient.Id)
            .ToListAsync(cancellationToken);

        return new ImportDetails
        {
            Import = summary,
            SourceResources = sourceResources,
            PatientIds = patientIds,
        };
    }

    public async Task<PatientRecord?> GetPatientRecordAsync(
        PatientId patientId, CancellationToken cancellationToken = default)
    {
        Patient? patient = await context.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        List<PrimaryCancerDiagnosis> diagnoses = await context.PrimaryCancerDiagnoses
            .AsNoTracking()
            .Where(diagnosis => diagnosis.PatientId == patientId)
            .OrderBy(diagnosis => diagnosis.Id)
            .ToListAsync(cancellationToken);

        List<CancerStaging> stagings = await context.CancerStagings
            .AsNoTracking()
            .Where(staging => staging.PatientId == patientId)
            .OrderBy(staging => staging.Id)
            .ToListAsync(cancellationToken);

        List<CancerSurgicalProcedure> procedures = await context.CancerSurgicalProcedures
            .AsNoTracking()
            .Where(procedure => procedure.PatientId == patientId)
            .OrderBy(procedure => procedure.Id)
            .ToListAsync(cancellationToken);

        return new PatientRecord
        {
            Patient = patient,
            PrimaryCancerDiagnoses = diagnoses,
            CancerStagings = stagings,
            CancerSurgicalProcedures = procedures,
        };
    }

    public async Task<IReadOnlyList<Finding>?> GetFindingsAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default)
    {
        bool exists = await context.ImportBatches
            .AsNoTracking()
            .AnyAsync(batch => batch.Id == batchId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        List<Finding> findings = await context.Findings
            .AsNoTracking()
            .Where(finding =>
                EF.Property<ImportBatchId>(finding, CanonicalColumns.BatchIdProperty) == batchId)
            .ToListAsync(cancellationToken);

        return
        [
            .. findings
                .OrderBy(finding => finding.CheckId.Value, StringComparer.Ordinal)
                .ThenBy(finding => finding.Target.Id),
        ];
    }

    public async Task<IReadOnlyList<Lineage>> GetProvenanceAsync(
        Guid domainEntityId, CancellationToken cancellationToken = default)
    {
        List<Lineage> records = await context.Lineages
            .AsNoTracking()
            .Where(lineage => lineage.DomainEntityId == domainEntityId)
            .ToListAsync(cancellationToken);

        return
        [
            .. records
                .OrderBy(lineage => lineage.IsWholeEntity ? 0 : 1)
                .ThenBy(lineage => lineage.FieldPath, StringComparer.Ordinal)
                .ThenBy(lineage => lineage.SourceResourceId.Value),
        ];
    }
}
