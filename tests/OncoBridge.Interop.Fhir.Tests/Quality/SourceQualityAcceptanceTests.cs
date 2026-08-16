using OncoBridge.Application.Normalization;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

public sealed class SourceQualityAcceptanceTests
{
    private static IngestedBundle AcceptanceBundle() =>
        NormalizationFixtures.Ingest(SyntheticFixtures.Phase4Bundle("bundle-acceptance-defects"));

    private static (string Check, Guid Target, string Message, string? Expected, string? Actual)[]
        Shape(SourceQualityAssessment assessment) =>
        [
            .. assessment.Findings.Select(finding => (
                finding.CheckId.Value,
                finding.Target.Id,
                finding.Message,
                finding.Expected,
                finding.Actual)),
        ];

    [Fact]
    public void The_acceptance_bundle_carries_seven_source_resources() =>
        Assert.Equal(7, AcceptanceBundle().SourceResources.Count);

    [Fact]
    public void The_acceptance_bundle_still_normalizes_to_the_four_canonical_concepts()
    {
        NormalizationResult result =
            new FhirNormalizer().Normalize(AcceptanceBundle().SourceResources);

        Assert.Single(result.Patients);
        Assert.Single(result.PrimaryCancerDiagnoses);
        Assert.Single(result.CancerSurgicalProcedures);

        CancerStaging staging = Assert.Single(result.CancerStagings);

        Assert.Equal(3, staging.Categories.Count);
        Assert.NotNull(staging.PrimaryTumour);
        Assert.NotNull(staging.RegionalNodes);
        Assert.NotNull(staging.DistantMetastases);
    }

    [Fact]
    public void The_acceptance_bundle_reports_exactly_the_three_deliberate_source_defects()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(AcceptanceBundle().SourceResources);

        Assert.Equal(3, assessment.Findings.Count);
        Assert.Equal(
            ["OB-CONF-001", "OB-CONF-002", "OB-REF-001"],
            QualityFixtures.CheckIdsOf(assessment).Order());
    }

    [Fact]
    public void The_acceptance_bundle_reports_no_structural_or_subject_disagreement_defect()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(AcceptanceBundle().SourceResources);

        Assert.Empty(QualityFixtures.FindingsFor(assessment, V1CheckIds.UnparseableEntry));
        Assert.Empty(
            QualityFixtures.FindingsFor(assessment, V1CheckIds.StageGroupSubjectDisagreement));
    }

    [Fact]
    public void Each_acceptance_finding_targets_the_source_resource_it_is_about()
    {
        IngestedBundle ingested = AcceptanceBundle();
        SourceQualityAssessment assessment = QualityFixtures.Assess(ingested.SourceResources);

        Guid Source(string logicalId) =>
            ingested.SourceResources.Single(source => source.SourceLogicalId == logicalId).Id.Value;

        Assert.Equal(
            Source("condition-001"),
            Assert.Single(QualityFixtures.FindingsFor(
                assessment, V1CheckIds.PrimaryCancerConditionCategory)).Target.Id);
        Assert.Equal(
            Source("staging-group-001"),
            Assert.Single(QualityFixtures.FindingsFor(
                assessment, V1CheckIds.StageGroupMethod)).Target.Id);
        Assert.Equal(
            Source("procedure-001"),
            Assert.Single(QualityFixtures.FindingsFor(
                assessment, V1CheckIds.UnresolvedReference)).Target.Id);
    }

    [Fact]
    public void Every_finding_carries_a_citation_and_targets_a_source_resource()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(AcceptanceBundle().SourceResources);

        Assert.All(
            assessment.Findings,
            finding =>
            {
                Assert.False(string.IsNullOrWhiteSpace(finding.Citation));
                Assert.Equal(FindingTargetKind.SourceResource, finding.Target.Kind);
                Assert.Null(finding.Target.DomainEntityType);
            });
    }

    [Fact]
    public void Assessing_the_same_source_resources_twice_gives_the_same_ordered_findings()
    {
        IReadOnlyList<SourceResource> sourceResources = AcceptanceBundle().SourceResources;

        Assert.Equal(
            Shape(QualityFixtures.Assess(sourceResources)),
            Shape(QualityFixtures.Assess(sourceResources)));
    }

    [Fact]
    public void Findings_are_ordered_by_the_entry_index_of_the_resource_they_are_about()
    {
        IngestedBundle ingested = AcceptanceBundle();
        SourceQualityAssessment assessment = QualityFixtures.Assess(ingested.SourceResources);

        int[] entryIndexes =
        [
            .. assessment.Findings.Select(finding =>
                ingested.SourceResources.Single(source => source.Id.Value == finding.Target.Id)
                    .EntryIndex),
        ];

        Assert.Equal(entryIndexes.Order(), entryIndexes);
    }

    [Fact]
    public void Reversing_the_input_order_does_not_reorder_the_findings()
    {
        IReadOnlyList<SourceResource> sourceResources = AcceptanceBundle().SourceResources;

        Assert.Equal(
            Shape(QualityFixtures.Assess(sourceResources)),
            Shape(QualityFixtures.Assess([.. sourceResources.Reverse()])));
    }
}
