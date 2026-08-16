using Microsoft.EntityFrameworkCore;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ReNormalizationTests(PostgreSqlFixture postgres)
{
    private sealed record SourceEvidence(
        Guid Id,
        string? ResourceType,
        string? SourceLogicalId,
        string? FullUrl,
        ContentHash? ContentHash,
        string? ResourceJson,
        int EntryIndex);

    private static SourceEvidence[] EvidenceOf(IEnumerable<SourceResource> sources) =>
    [
        .. sources.Select(source => new SourceEvidence(
            source.Id.Value,
            source.ResourceType,
            source.SourceLogicalId,
            source.FullUrl,
            source.ContentHash,
            source.ResourceJson,
            source.EntryIndex)),
    ];

    [Fact]
    public async Task Re_normalizing_a_batch_replaces_the_derived_tier_rather_than_appending()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);
        CanonicalCounts first = await scenario.CountsAsync();

        await scenario.NormalizeAsync(batchId);

        Assert.Equal(first, await scenario.CountsAsync());
        Assert.Equal(new CanonicalCounts(1, 1, 1, 3, 1, 7), await scenario.CountsAsync());
    }

    [Fact]
    public async Task Re_normalizing_a_batch_keeps_every_derived_identity_stable()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);

        Guid patient = (await scenario.SinglePatientAsync()).Id.Value;
        Guid diagnosis = (await scenario.SingleDiagnosisAsync()).Id.Value;
        Guid staging = (await scenario.SingleStagingAsync()).Id;
        Guid procedure = (await scenario.SingleProcedureAsync()).Id;

        await scenario.NormalizeAsync(batchId);

        Assert.Equal(patient, (await scenario.SinglePatientAsync()).Id.Value);
        Assert.Equal(diagnosis, (await scenario.SingleDiagnosisAsync()).Id.Value);
        Assert.Equal(staging, (await scenario.SingleStagingAsync()).Id);
        Assert.Equal(procedure, (await scenario.SingleProcedureAsync()).Id);
    }

    [Fact]
    public async Task Re_normalizing_a_batch_updates_the_normalization_instant()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);
        scenario.Clock.Advance(TimeSpan.FromHours(3));
        await scenario.NormalizeAsync(batchId);

        Assert.Equal(
            NormalizationScenario.NormalizedAt.AddHours(3),
            (await scenario.ReloadBatchAsync(batchId)).NormalizedAt);
    }

    [Fact]
    public async Task Re_normalizing_a_batch_leaves_every_piece_of_source_evidence_untouched()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);

        ImportBatch before = await scenario.ReloadBatchAsync(batchId);
        byte[] payload = before.RawPayload.ToArray();
        SourceEvidence[] sources = EvidenceOf(await scenario.ReloadSourcesAsync(batchId));

        await scenario.NormalizeAsync(batchId);

        ImportBatch after = await scenario.ReloadBatchAsync(batchId);

        Assert.Equal(payload, after.RawPayload.ToArray());
        Assert.Equal(SyntheticFixtures.CompleteNormalizationBundleBytes, after.RawPayload.ToArray());
        Assert.Equal(before.ContentHash, after.ContentHash);
        Assert.True(after.VerifyPayloadIntegrity());
        Assert.Equal(before.ReceivedAt, after.ReceivedAt);
        Assert.Equal(before.EntryCount, after.EntryCount);
        Assert.Equal(sources, EvidenceOf(await scenario.ReloadSourcesAsync(batchId)));
    }

    [Fact]
    public async Task Re_normalizing_one_batch_does_not_touch_another_batch()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId first = await scenario.IngestCompleteBundleAsync("batch-a");
        ImportBatchId second = await scenario.IngestCompleteBundleAsync("batch-b");

        await scenario.NormalizeAsync(first);
        await scenario.NormalizeAsync(second);

        Guid[] secondPatients = await PatientsOfAsync(scenario, second);
        Guid[] secondLineage = await LineageEntitiesOfAsync(scenario, second);

        Assert.Equal(new CanonicalCounts(2, 2, 2, 6, 2, 14), await scenario.CountsAsync());

        await scenario.NormalizeAsync(first);

        Assert.Equal(new CanonicalCounts(2, 2, 2, 6, 2, 14), await scenario.CountsAsync());
        Assert.Equal(secondPatients, await PatientsOfAsync(scenario, second));
        Assert.Equal(secondLineage, await LineageEntitiesOfAsync(scenario, second));
        Assert.Equal(
            NormalizationScenario.NormalizedAt,
            (await scenario.ReloadBatchAsync(second)).NormalizedAt);
    }

    [Fact]
    public async Task A_failed_replacement_leaves_the_previous_derived_state_intact()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);

        CanonicalCounts before = await scenario.CountsAsync();
        Guid patient = (await scenario.SinglePatientAsync()).Id.Value;
        DateTimeOffset? normalizedAt = (await scenario.ReloadBatchAsync(batchId)).NormalizedAt;
        SourceEvidence[] sources = EvidenceOf(await scenario.ReloadSourcesAsync(batchId));

        scenario.Clock.Advance(TimeSpan.FromHours(5));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => scenario.NormalizeAsync(batchId, new DanglingLineageNormalizer()));

        scenario.Context.ChangeTracker.Clear();

        Assert.Equal(before, await scenario.CountsAsync());
        Assert.Equal(patient, (await scenario.SinglePatientAsync()).Id.Value);
        Assert.Equal(3, await scenario.StageCategoryCountAsync());
        Assert.Equal(normalizedAt, (await scenario.ReloadBatchAsync(batchId)).NormalizedAt);
        Assert.Equal(sources, EvidenceOf(await scenario.ReloadSourcesAsync(batchId)));
    }

    private static async Task<Guid[]> PatientsOfAsync(
        NormalizationScenario scenario, ImportBatchId batchId) =>
        [
            .. (await scenario.ReloadSourcesAsync(batchId))
                .Where(source => source.ResourceType == "Patient")
                .Select(source => source.Id.Value)
                .Order(),
        ];

    private static async Task<Guid[]> LineageEntitiesOfAsync(
        NormalizationScenario scenario, ImportBatchId batchId)
    {
        HashSet<SourceResourceId> sources =
            [.. (await scenario.ReloadSourcesAsync(batchId)).Select(source => source.Id)];

        return
        [
            .. (await scenario.LineageAsync())
                .Where(record => sources.Contains(record.SourceResourceId))
                .Select(record => record.DomainEntityId)
                .Order(),
        ];
    }

}
