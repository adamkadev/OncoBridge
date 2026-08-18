using OncoBridge.Application.Imports;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

public sealed class ConformanceQualityTests
{
    private static SourceQualityAssessment AssessConditionWith(params string[] members) =>
        QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                [NormalizationFixtures.BreastCancerCode, .. members]));

    private static Finding[] CategoryFindings(SourceQualityAssessment assessment) =>
        QualityFixtures.FindingsFor(assessment, V1CheckIds.PrimaryCancerConditionCategory);

    private static Finding[] MethodFindings(SourceQualityAssessment assessment) =>
        QualityFixtures.FindingsFor(assessment, V1CheckIds.StageGroupMethod);

    [Fact]
    public void A_profiled_primary_cancer_condition_without_any_category_is_reported()
    {
        Finding finding = Assert.Single(CategoryFindings(AssessConditionWith()));

        Assert.Equal(FindingCategory.Conformance, finding.Category);
        Assert.Equal(FindingSeverity.Error, finding.Severity);
        Assert.Equal("no Condition.category coding is stated", finding.Actual);
        Assert.Equal(
            "https://hl7.org/fhir/us/mcode/STU4/StructureDefinition-mcode-primary-cancer-condition"
                + "-definitions.html",
            finding.Citation);
    }

    [Fact]
    public void A_problem_list_item_category_satisfies_the_mandatory_slice() =>
        Assert.Empty(CategoryFindings(
            AssessConditionWith(QualityFixtures.ProblemListItemCategory)));

    [Fact]
    public void A_us_core_health_concern_category_satisfies_the_mandatory_slice() =>
        Assert.Empty(CategoryFindings(
            AssessConditionWith(QualityFixtures.HealthConcernCategory)));

    [Fact]
    public void An_encounter_diagnosis_category_alone_does_not_satisfy_the_mandatory_slice()
    {
        Finding finding = Assert.Single(CategoryFindings(
            AssessConditionWith(QualityFixtures.EncounterDiagnosisCategory)));

        Assert.Equal(
            "stated Condition.category codings: "
                + "http://terminology.hl7.org/CodeSystem/condition-category|encounter-diagnosis",
            finding.Actual);
    }

    [Fact]
    public void A_condition_stating_another_category_beside_the_required_one_is_not_reported() =>
        Assert.Empty(CategoryFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode,
                """
                "category":[
                  {"coding":[{"system":"http://terminology.hl7.org/CodeSystem/condition-category",
                              "code":"encounter-diagnosis"}]},
                  {"coding":[{"system":"http://terminology.hl7.org/CodeSystem/condition-category",
                              "code":"problem-list-item"}]}]
                """))));

    [Fact]
    public void A_condition_that_declares_no_mcode_profile_is_not_judged_by_this_mcode_check() =>
        Assert.Empty(CategoryFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.ConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.BreastCancerCode,
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl)))));

    [Fact]
    public void The_category_finding_targets_the_condition_source_resource()
    {
        IngestedPayload ingested = NormalizationFixtures.Ingest(
            SyntheticFixtures.Phase4Bundle("bundle-primary-cancer-missing-category"));

        Finding finding = Assert.Single(
            CategoryFindings(QualityFixtures.Assess(ingested.SourceResources)));

        SourceResource condition =
            ingested.SourceResources.Single(source => source.ResourceType == "Condition");

        Assert.Equal(condition.Id.Value, finding.Target.Id);
        Assert.Equal(FindingTargetKind.SourceResource, finding.Target.Kind);
    }

    [Fact]
    public void A_recognised_stage_group_without_a_method_is_reported()
    {
        Finding finding = Assert.Single(MethodFindings(QualityFixtures.AssessBundle(
            SyntheticFixtures.Phase4Bundle("bundle-stage-group-missing-method"))));

        Assert.Equal(FindingCategory.Conformance, finding.Category);
        Assert.Equal(FindingSeverity.Error, finding.Severity);
        Assert.Equal("Observation.method is absent", finding.Actual);
        Assert.Equal(
            "https://hl7.org/fhir/us/mcode/STU4/StructureDefinition-mcode-tnm-stage-group.html",
            finding.Citation);
    }

    [Fact]
    public void A_stage_group_stating_a_method_is_not_reported() =>
        Assert.Empty(MethodFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            QualityFixtures.StageGroup())));

    [Fact]
    public void The_method_check_does_not_validate_the_terminology_of_the_method_it_finds() =>
        Assert.Empty(MethodFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            StagingFixtures.LinkedStageGroupEntry(NormalizationFixtures.Method(
                "urn:oncobridge:synthetic:method", "not-a-real-staging-method")))));

    [Fact]
    public void The_method_finding_targets_the_stage_group_source_resource()
    {
        IngestedPayload ingested = NormalizationFixtures.Ingest(
            SyntheticFixtures.Phase4Bundle("bundle-stage-group-missing-method"));

        Finding finding = Assert.Single(
            MethodFindings(QualityFixtures.Assess(ingested.SourceResources)));

        SourceResource stageGroup = ingested.SourceResources
            .Single(source => source.SourceLogicalId == "staging-group-001");

        Assert.Equal(stageGroup.Id.Value, finding.Target.Id);
    }

    [Fact]
    public void An_observation_that_is_not_a_recognised_stage_group_is_not_judged_for_method() =>
        Assert.Empty(MethodFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            StagingFixtures.PrimaryTumourEntry())));
}
