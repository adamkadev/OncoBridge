using Microsoft.EntityFrameworkCore;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Infrastructure.Tests;

internal sealed class NormalizationScenario(OncoBridgeDbContext context, FixedTimeProvider clock)
{
    internal static readonly DateTimeOffset ReceivedAt =
        new(2026, 8, 15, 9, 30, 0, TimeSpan.FromHours(2));

    internal static readonly DateTimeOffset NormalizedAt =
        new(2026, 8, 16, 11, 0, 0, TimeSpan.Zero);

    internal OncoBridgeDbContext Context => context;

    internal FixedTimeProvider Clock => clock;

    internal static async Task<NormalizationScenario> StartAsync(PostgreSqlFixture postgres) =>
        new(await postgres.CreateMigratedContextAsync(), new FixedTimeProvider(NormalizedAt));

    internal async Task<ImportBatchId> IngestAsync(byte[] payload, string label)
    {
        IngestedBundle ingested = new FhirBundleIngestor().Ingest(payload, label, ReceivedAt);

        await new ImportBatchStore(context).SaveAsync(ingested.Batch, ingested.SourceResources);
        context.ChangeTracker.Clear();

        return ingested.Batch.Id;
    }

    internal Task<ImportBatchId> IngestCompleteBundleAsync(string label = "phase3d-fixture") =>
        IngestAsync(SyntheticFixtures.CompleteNormalizationBundleBytes, label);

    internal Task<NormalizationResult?> NormalizeAsync(ImportBatchId batchId) =>
        NormalizeAsync(batchId, new FhirNormalizer());

    internal async Task<NormalizationResult?> NormalizeAsync(
        ImportBatchId batchId, ICanonicalNormalizer normalizer)
    {
        NormalizationResult? result =
            await new NormalizeImportBatch(normalizer, new NormalizationStore(context), clock)
                .ExecuteAsync(batchId);

        context.ChangeTracker.Clear();

        return result;
    }

    internal Task<ImportBatch> ReloadBatchAsync(ImportBatchId batchId) =>
        context.ImportBatches.AsNoTracking().SingleAsync(batch => batch.Id == batchId);

    internal Task<List<SourceResource>> ReloadSourcesAsync(ImportBatchId batchId) =>
        context.SourceResources
            .AsNoTracking()
            .Where(resource => resource.BatchId == batchId)
            .OrderBy(resource => resource.EntryIndex)
            .ToListAsync();

    internal Task<Patient> SinglePatientAsync() => context.Patients.AsNoTracking().SingleAsync();

    internal Task<PrimaryCancerDiagnosis> SingleDiagnosisAsync() =>
        context.PrimaryCancerDiagnoses.AsNoTracking().SingleAsync();

    internal Task<CancerStaging> SingleStagingAsync() =>
        context.CancerStagings.AsNoTracking().SingleAsync();

    internal Task<CancerSurgicalProcedure> SingleProcedureAsync() =>
        context.CancerSurgicalProcedures.AsNoTracking().SingleAsync();

    internal Task<List<Lineage>> LineageAsync() => context.Lineages.AsNoTracking().ToListAsync();

    internal Task<int> StageCategoryCountAsync() => context.Set<StageCategory>().CountAsync();

    internal async Task<CanonicalCounts> CountsAsync() => new(
        await context.Patients.CountAsync(),
        await context.PrimaryCancerDiagnoses.CountAsync(),
        await context.CancerStagings.CountAsync(),
        await StageCategoryCountAsync(),
        await context.CancerSurgicalProcedures.CountAsync(),
        await context.Lineages.CountAsync());
}

internal readonly record struct CanonicalCounts(
    int Patients,
    int Diagnoses,
    int Stagings,
    int Categories,
    int Procedures,
    int Lineage);
