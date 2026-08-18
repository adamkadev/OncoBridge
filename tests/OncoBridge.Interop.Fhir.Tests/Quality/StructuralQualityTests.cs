using OncoBridge.Application.Imports;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

public sealed class StructuralQualityTests
{
    private static IngestedPayload MalformedBundle() =>
        NormalizationFixtures.Ingest(SyntheticFixtures.Phase4Bundle("bundle-structural-malformed"));

    [Fact]
    public void An_entry_that_is_not_a_known_R4_resource_produces_one_structural_error()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(MalformedBundle().SourceResources);

        Finding finding = Assert.Single(
            QualityFixtures.FindingsFor(assessment, V1CheckIds.UnparseableEntry));

        Assert.Equal(FindingCategory.Structural, finding.Category);
        Assert.Equal(FindingSeverity.Error, finding.Severity);
        Assert.Equal("The bundle entry could not be parsed as a known FHIR R4 resource.", finding.Message);
    }

    [Fact]
    public void The_structural_finding_targets_the_source_resource_that_could_not_be_parsed()
    {
        IngestedPayload ingested = MalformedBundle();

        Finding finding = Assert.Single(QualityFixtures.FindingsFor(
            QualityFixtures.Assess(ingested.SourceResources), V1CheckIds.UnparseableEntry));

        SourceResource malformed =
            ingested.SourceResources.Single(source => source.EntryIndex == 1);

        Assert.Equal(FindingTargetKind.SourceResource, finding.Target.Kind);
        Assert.Equal(malformed.Id.Value, finding.Target.Id);
    }

    [Fact]
    public void The_structural_finding_cites_the_R4_json_representation_and_never_a_parser_message()
    {
        Finding finding = Assert.Single(QualityFixtures.FindingsFor(
            QualityFixtures.Assess(MalformedBundle().SourceResources), V1CheckIds.UnparseableEntry));

        Assert.Equal("https://hl7.org/fhir/R4/json.html", finding.Citation);
        Assert.Equal("a deserializable FHIR R4 resource", finding.Expected);
        Assert.Equal("an entry stating resourceType 'NotAKnownFhirResource'", finding.Actual);
    }

    [Fact]
    public void A_malformed_entry_does_not_stop_its_siblings_being_assessed()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(MalformedBundle().SourceResources);

        Assert.Single(QualityFixtures.FindingsFor(assessment, V1CheckIds.UnparseableEntry));
        Assert.NotEmpty(assessment.CoverageNotes);
    }

    [Fact]
    public void A_valid_R4_resource_outside_V1_coverage_is_not_called_structurally_invalid()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.Assess(MalformedBundle().SourceResources);

        Assert.Single(QualityFixtures.FindingsFor(assessment, V1CheckIds.UnparseableEntry));
        Assert.Contains(assessment.CoverageNotes, note => note.Subject == "MedicationRequest");
    }

    [Fact]
    public void An_entry_carrying_no_resource_content_is_structurally_unparseable()
    {
        SourceResource empty = new(
            SourceResourceId.New(), ImportBatchId.New(), entryIndex: 0, resourceType: "Patient");

        Finding finding = Assert.Single(QualityFixtures.Assess([empty]).Findings);

        Assert.Equal(V1CheckIds.UnparseableEntry, finding.CheckId);
        Assert.Equal("the entry carries no resource content", finding.Actual);
    }

    [Fact]
    public void A_clean_source_bundle_produces_no_structural_finding()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.AssessBundle(SyntheticFixtures.Phase4Bundle("bundle-clean-source"));

        Assert.Empty(QualityFixtures.FindingsFor(assessment, V1CheckIds.UnparseableEntry));
    }
}
