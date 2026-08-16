using Microsoft.EntityFrameworkCore;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Quality;
using OncoBridge.Infrastructure.Persistence;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class QualityPersistenceTests(PostgreSqlFixture postgres)
{
    private const string StagingPrecedesDiagnosis = "bundle-staging-precedes-diagnosis";

    private const string AcceptanceDefects = "bundle-acceptance-defects";

    private static string[] CheckIdsOf(IEnumerable<Finding> findings) =>
        [.. findings.Select(finding => finding.CheckId.Value)];

    private async Task<(NormalizationScenario Scenario, ImportBatchId BatchId)> NormalizedAsync(
        string fixture, string label = "phase4b-fixture")
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        ImportBatchId batchId = await scenario.IngestPhase4BundleAsync(fixture, label);

        await scenario.NormalizeAsync(batchId);

        return (scenario, batchId);
    }

    [Fact]
    public async Task A_source_targeted_finding_round_trips_through_postgresql()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestPhase4BundleAsync(AcceptanceDefects);
        SourceResourceId target = (await scenario.ReloadSourcesAsync(batchId))[0].Id;

        Finding expected = Finding.Create(
            V1CheckIds.PrimaryCancerConditionCategory,
            FindingCategory.Conformance,
            FindingSeverity.Error,
            "A deterministic message.",
            FindingTarget.ForSourceResource(target),
            "https://example.invalid/spec",
            expected: "something",
            actual: "something else");

        await new QualityStore(scenario.Context).ReplaceFindingsAsync(batchId, [expected]);
        scenario.Context.ChangeTracker.Clear();

        Finding reloaded = Assert.Single(await scenario.FindingsAsync());

        Assert.Equal(expected, reloaded);
        Assert.Equal(FindingTargetKind.SourceResource, reloaded.Target.Kind);
        Assert.Null(reloaded.Target.DomainEntityType);
    }

    [Fact]
    public async Task A_domain_entity_targeted_finding_round_trips_through_postgresql()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestPhase4BundleAsync(AcceptanceDefects);

        Finding expected = Finding.Create(
            V1CheckIds.StagingPrecedesDiagnosis,
            FindingCategory.DomainConsistency,
            FindingSeverity.Warning,
            "A deterministic message.",
            FindingTarget.ForDomainEntity(nameof(CancerStaging), Guid.NewGuid()),
            DomainQualityCitations.VariablePrecisionTemporalModel);

        await new QualityStore(scenario.Context).ReplaceFindingsAsync(batchId, [expected]);
        scenario.Context.ChangeTracker.Clear();

        Finding reloaded = Assert.Single(await scenario.FindingsAsync());

        Assert.Equal(expected, reloaded);
        Assert.Equal(FindingTargetKind.DomainEntity, reloaded.Target.Kind);
        Assert.Equal(nameof(CancerStaging), reloaded.Target.DomainEntityType);
        Assert.Null(reloaded.Expected);
        Assert.Null(reloaded.Actual);
    }

    [Fact]
    public async Task A_finding_whose_target_shape_contradicts_its_kind_is_refused_by_the_database()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestPhase4BundleAsync(AcceptanceDefects);

        Finding finding = Finding.Create(
            V1CheckIds.StagingPrecedesDiagnosis,
            FindingCategory.DomainConsistency,
            FindingSeverity.Warning,
            "A deterministic message.",
            FindingTarget.ForDomainEntity(nameof(CancerStaging), Guid.NewGuid()),
            DomainQualityCitations.VariablePrecisionTemporalModel);

        scenario.Context.Add(finding).Property("BatchId").CurrentValue = batchId;
        scenario.Context.Entry(finding).ComplexProperty(nameof(Finding.Target))
            .Property(nameof(FindingTarget.DomainEntityType)).CurrentValue = null;

        await Assert.ThrowsAsync<DbUpdateException>(() => scenario.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Assessing_a_staging_that_precedes_its_diagnosis_persists_one_domain_finding()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) =
            await NormalizedAsync(StagingPrecedesDiagnosis);
        await using OncoBridgeDbContext _context = scenario.Context;

        QualityAssessment assessment = (await scenario.AssessAsync(batchId))!;

        Finding finding = Assert.Single(assessment.Findings);
        Finding persisted = Assert.Single(await scenario.FindingsAsync());

        Assert.Equal(V1CheckIds.StagingPrecedesDiagnosis, finding.CheckId);
        Assert.Equal(FindingCategory.DomainConsistency, persisted.Category);
        Assert.Equal(FindingSeverity.Warning, persisted.Severity);
        Assert.Equal((await scenario.SingleStagingAsync()).Id, persisted.Target.Id);
        Assert.Equal(nameof(CancerStaging), persisted.Target.DomainEntityType);
        Assert.Equal("staging effective: 2019-05-01; diagnosis onset: 2019-06", persisted.Actual);
    }

    [Fact]
    public async Task The_isolating_domain_fixture_fires_no_source_check()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) =
            await NormalizedAsync(StagingPrecedesDiagnosis);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);

        Assert.Equal(["OB-DOM-001"], CheckIdsOf(await scenario.FindingsAsync()));
    }

    [Fact]
    public async Task The_phase_4a_acceptance_bundle_still_persists_exactly_its_three_source_defects()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedAsync(AcceptanceDefects);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);

        Assert.Equal(
            ["OB-CONF-001", "OB-CONF-002", "OB-REF-001"], CheckIdsOf(await scenario.FindingsAsync()));
    }

    [Fact]
    public async Task Assessing_the_same_batch_twice_replaces_rather_than_accumulates()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedAsync(AcceptanceDefects);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);
        List<Finding> first = await scenario.FindingsAsync();

        await scenario.AssessAsync(batchId);
        List<Finding> second = await scenario.FindingsAsync();

        Assert.Equal(3, second.Count);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Source_findings_survive_a_normalization_rerun_that_is_not_reassessed()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedAsync(AcceptanceDefects);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);
        List<Finding> before = await scenario.FindingsAsync();

        await scenario.NormalizeAsync(batchId);

        Assert.Equal(before, await scenario.FindingsAsync());
    }

    [Fact]
    public async Task A_domain_finding_is_invalidated_by_a_normalization_rerun()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) =
            await NormalizedAsync(StagingPrecedesDiagnosis);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);
        Assert.Single(await scenario.FindingsAsync());

        await scenario.NormalizeAsync(batchId);

        Assert.Empty(await scenario.FindingsAsync());
    }

    [Fact]
    public async Task Reassessing_after_a_rerun_recomputes_the_domain_finding_without_duplicating_it()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) =
            await NormalizedAsync(StagingPrecedesDiagnosis);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);
        Finding before = Assert.Single(await scenario.FindingsAsync());

        await scenario.NormalizeAsync(batchId);
        await scenario.AssessAsync(batchId);

        Assert.Equal(before, Assert.Single(await scenario.FindingsAsync()));
    }

    [Fact]
    public async Task Re_normalizing_one_batch_touches_neither_kind_of_finding_in_another()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId first = await scenario.IngestPhase4BundleAsync(AcceptanceDefects, "batch-a");
        ImportBatchId second =
            await scenario.IngestPhase4BundleAsync(StagingPrecedesDiagnosis, "batch-b");

        await scenario.NormalizeAsync(first);
        await scenario.AssessAsync(first);
        await scenario.NormalizeAsync(second);
        await scenario.AssessAsync(second);

        Guid[] secondTargets = await scenario.TargetsOfBatchAsync(second);
        List<Finding> secondFindings = await scenario.FindingsAboutAsync(secondTargets);

        Assert.Equal(["OB-DOM-001"], CheckIdsOf(secondFindings));
        Assert.Equal(4, (await scenario.FindingsAsync()).Count);

        await scenario.NormalizeAsync(first);

        Assert.Equal(secondFindings, await scenario.FindingsAboutAsync(secondTargets));
        Assert.Equal(
            ["OB-CONF-001", "OB-CONF-002", "OB-REF-001"],
            CheckIdsOf(await scenario.FindingsAboutAsync(await scenario.TargetsOfBatchAsync(first))));
    }

    [Fact]
    public async Task A_failed_normalization_leaves_the_previous_domain_finding_in_place()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) =
            await NormalizedAsync(StagingPrecedesDiagnosis);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);

        List<Finding> before = await scenario.FindingsAsync();
        CanonicalCounts counts = await scenario.CountsAsync();
        DateTimeOffset? normalizedAt = (await scenario.ReloadBatchAsync(batchId)).NormalizedAt;

        scenario.Clock.Advance(TimeSpan.FromHours(4));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => scenario.NormalizeAsync(batchId, new DanglingLineageNormalizer()));

        scenario.Context.ChangeTracker.Clear();

        Assert.Equal(before, await scenario.FindingsAsync());
        Assert.Equal(counts, await scenario.CountsAsync());
        Assert.Equal(normalizedAt, (await scenario.ReloadBatchAsync(batchId)).NormalizedAt);
    }

    [Fact]
    public async Task A_failed_normalization_leaves_the_previous_source_findings_in_place()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedAsync(AcceptanceDefects);
        await using OncoBridgeDbContext _context = scenario.Context;

        await scenario.AssessAsync(batchId);
        List<Finding> before = await scenario.FindingsAsync();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => scenario.NormalizeAsync(batchId, new DanglingLineageNormalizer()));

        scenario.Context.ChangeTracker.Clear();

        Assert.Equal(before, await scenario.FindingsAsync());
    }

    [Fact]
    public async Task An_unnormalized_batch_is_assessed_for_source_quality_alone()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestPhase4BundleAsync(AcceptanceDefects);

        QualityAssessment assessment = (await scenario.AssessAsync(batchId))!;

        Assert.Equal(
            ["OB-CONF-001", "OB-CONF-002", "OB-REF-001"], CheckIdsOf(await scenario.FindingsAsync()));
        Assert.DoesNotContain(
            assessment.Findings, finding => finding.Category == FindingCategory.DomainConsistency);
        Assert.Equal(new CanonicalCounts(0, 0, 0, 0, 0, 0), await scenario.CountsAsync());
    }

    [Fact]
    public async Task Assessing_a_batch_that_does_not_exist_returns_nothing_and_writes_nothing()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Null(await scenario.AssessAsync(ImportBatchId.New()));
        Assert.Empty(await scenario.FindingsAsync());
    }
}
