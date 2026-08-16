using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class PrimaryCancerDiagnosisNormalizationTests
{
    private static PrimaryCancerDiagnosis NormalizeDiagnosisWith(params string[] members)
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                members));

        return Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_profiled_primary_cancer_condition_becomes_a_primary_cancer_diagnosis()
    {
        NormalizationResult result = NormalizationFixtures.NormalizePrimaryCancerBundle();

        PrimaryCancerDiagnosis diagnosis = Assert.Single(result.PrimaryCancerDiagnoses);

        Assert.Equal(Assert.Single(result.Patients).Id, diagnosis.PatientId);
    }

    [Fact]
    public void The_diagnosis_code_preserves_system_code_and_display_exactly()
    {
        PrimaryCancerDiagnosis diagnosis =
            Assert.Single(NormalizationFixtures.NormalizePrimaryCancerBundle().PrimaryCancerDiagnoses);

        Assert.Equal(
            new CodedConcept(
                "http://snomed.info/sct", "254837009", "Malignant neoplasm of breast (disorder)"),
            diagnosis.Code);
    }

    [Fact]
    public void The_first_coding_carrying_both_a_system_and_a_code_is_selected()
    {
        PrimaryCancerDiagnosis diagnosis = NormalizeDiagnosisWith(
            """
            "code":{"coding":[
                {"display":"No system and no code"},
                {"system":"http://snomed.info/sct"},
                {"code":"254837009"},
                {"system":"http://snomed.info/sct","code":"254837009","display":"Chosen"},
                {"system":"http://hl7.org/fhir/sid/icd-10-cm","code":"C50.9","display":"Later"}]}
            """);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "254837009", "Chosen"), diagnosis.Code);
    }

    [Fact]
    public void Coding_selection_repeats_the_same_choice_on_every_run()
    {
        const string Code =
            """
            "code":{"coding":[
                {"system":"http://snomed.info/sct","code":"254837009","display":"First"},
                {"system":"http://hl7.org/fhir/sid/icd-10-cm","code":"C50.9","display":"Second"}]}
            """;

        Assert.Equal(NormalizeDiagnosisWith(Code).Code, NormalizeDiagnosisWith(Code).Code);
    }

    [Fact]
    public void A_month_precision_onset_date_time_stays_at_month_precision()
    {
        PrimaryCancerDiagnosis diagnosis =
            Assert.Single(NormalizationFixtures.NormalizePrimaryCancerBundle().PrimaryCancerDiagnoses);

        Assert.NotNull(diagnosis.Onset);
        Assert.Equal(TemporalOccurrenceKind.Date, diagnosis.Onset.Kind);
        Assert.Equal(PartialDate.FromYearMonth(2019, 3), diagnosis.Onset.Date);
        Assert.Equal(DatePrecision.Month, diagnosis.Onset.Date!.Precision);
        Assert.Null(diagnosis.Onset.Date.Day);
    }

    [Fact]
    public void A_full_onset_time_stamp_keeps_its_instant_and_stated_offset()
    {
        PrimaryCancerDiagnosis diagnosis = NormalizeDiagnosisWith(
            NormalizationFixtures.BreastCancerCode,
            """ "onsetDateTime":"2019-03-14T10:00:00+02:00" """);

        PartialDate onset = diagnosis.Onset!.Date!;

        Assert.Equal(DatePrecision.Instant, onset.Precision);
        Assert.Equal(new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2)), onset.Instant);
        Assert.Equal(TimeSpan.FromHours(2), onset.Instant!.Value.Offset);
    }

    [Fact]
    public void An_onset_period_keeps_both_boundaries_at_their_own_precision()
    {
        PrimaryCancerDiagnosis diagnosis = NormalizeDiagnosisWith(
            NormalizationFixtures.BreastCancerCode,
            """ "onsetPeriod":{"start":"2019","end":"2020-06-15"} """);

        Assert.Equal(TemporalOccurrenceKind.Period, diagnosis.Onset!.Kind);

        PartialPeriod onset = diagnosis.Onset.Period!;

        Assert.Equal(PartialDate.FromYear(2019), onset.Start);
        Assert.Equal(DatePrecision.Year, onset.Start!.Precision);
        Assert.Equal(PartialDate.FromDate(2020, 6, 15), onset.End);
        Assert.Equal(DatePrecision.Day, onset.End!.Precision);
    }

    [Fact]
    public void An_onset_period_with_only_one_boundary_is_not_completed()
    {
        PrimaryCancerDiagnosis diagnosis = NormalizeDiagnosisWith(
            NormalizationFixtures.BreastCancerCode,
            """ "onsetPeriod":{"start":"2019-03"} """);

        PartialPeriod onset = diagnosis.Onset!.Period!;

        Assert.Equal(PartialDate.FromYearMonth(2019, 3), onset.Start);
        Assert.True(onset.IsUnboundedEnd);
    }

    [Fact]
    public void A_body_site_maps_when_present()
    {
        PrimaryCancerDiagnosis diagnosis =
            Assert.Single(NormalizationFixtures.NormalizePrimaryCancerBundle().PrimaryCancerDiagnoses);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "76752008", "Breast structure (body structure)"),
            diagnosis.BodySite);
    }

    [Fact]
    public void An_absent_body_site_is_not_invented()
    {
        Assert.Null(NormalizeDiagnosisWith(NormalizationFixtures.BreastCancerCode).BodySite);
    }

    [Fact]
    public void The_recorded_date_maps_at_the_precision_the_source_stated()
    {
        PrimaryCancerDiagnosis diagnosis =
            Assert.Single(NormalizationFixtures.NormalizePrimaryCancerBundle().PrimaryCancerDiagnoses);

        Assert.Equal(PartialDate.FromDate(2019, 4, 2), diagnosis.RecordedDate);
        Assert.Equal(DatePrecision.Day, diagnosis.RecordedDate!.Precision);
    }

    [Fact]
    public void A_partial_recorded_date_is_not_widened_to_a_full_date()
    {
        PrimaryCancerDiagnosis diagnosis = NormalizeDiagnosisWith(
            NormalizationFixtures.BreastCancerCode,
            """ "recordedDate":"2019-04" """);

        Assert.Equal(PartialDate.FromYearMonth(2019, 4), diagnosis.RecordedDate);
        Assert.Null(diagnosis.RecordedDate!.Day);
    }
}
