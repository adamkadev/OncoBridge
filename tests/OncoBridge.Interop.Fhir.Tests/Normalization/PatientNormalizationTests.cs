using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class PatientNormalizationTests
{
    private static Patient NormalizePatientWith(params string[] members)
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(
                NormalizationFixtures.PatientFullUrl, "patient-001", members),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        return Assert.Single(result.Patients);
    }

    [Fact]
    public void A_patient_referenced_by_an_eligible_primary_cancer_condition_is_normalized()
    {
        NormalizationResult result = NormalizationFixtures.NormalizePrimaryCancerBundle();

        Patient patient = Assert.Single(result.Patients);

        Assert.NotEqual(default, patient.Id.Value);
    }

    [Fact]
    public void A_patient_that_no_eligible_condition_references_is_not_normalized()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"));

        Assert.Empty(result.Patients);
    }

    [Fact]
    public void The_source_patient_identifier_becomes_the_source_identifier()
    {
        NormalizationResult result = NormalizationFixtures.NormalizePrimaryCancerBundle();

        Assert.Equal("SYN-0001", Assert.Single(result.Patients).SourceIdentifier);
    }

    [Fact]
    public void The_first_identifier_with_a_value_is_selected_in_source_order()
    {
        Patient patient = NormalizePatientWith(
            """
            "identifier":[{"system":"urn:oncobridge:synthetic:blank"},
                          {"system":"urn:oncobridge:synthetic:mrn","value":"SYN-0002"},
                          {"system":"urn:oncobridge:synthetic:other","value":"SYN-0003"}]
            """);

        Assert.Equal("SYN-0002", patient.SourceIdentifier);
    }

    [Fact]
    public void A_year_only_birth_date_stays_at_year_precision()
    {
        Patient patient = NormalizePatientWith(""" "birthDate":"1968" """);

        Assert.Equal(PartialDate.FromYear(1968), patient.BirthDate);
        Assert.Equal(DatePrecision.Year, patient.BirthDate!.Precision);
        Assert.Null(patient.BirthDate.Month);
        Assert.Null(patient.BirthDate.Day);
    }

    [Fact]
    public void A_year_and_month_birth_date_stays_at_month_precision()
    {
        Patient patient = NormalizePatientWith(""" "birthDate":"1968-07" """);

        Assert.Equal(PartialDate.FromYearMonth(1968, 7), patient.BirthDate);
        Assert.Equal(DatePrecision.Month, patient.BirthDate!.Precision);
        Assert.Null(patient.BirthDate.Day);
    }

    [Fact]
    public void A_full_birth_date_stays_at_day_precision()
    {
        Patient patient = NormalizePatientWith(""" "birthDate":"1968-07-14" """);

        Assert.Equal(PartialDate.FromDate(1968, 7, 14), patient.BirthDate);
        Assert.Equal(DatePrecision.Day, patient.BirthDate!.Precision);
    }

    [Fact]
    public void An_absent_birth_date_is_not_invented()
    {
        Assert.Null(NormalizePatientWith().BirthDate);
    }

    [Fact]
    public void Administrative_gender_is_not_mapped_to_sex_at_birth_as_recorded()
    {
        NormalizationResult result = NormalizationFixtures.NormalizePrimaryCancerBundle();

        Assert.Null(Assert.Single(result.Patients).SexAtBirthAsRecorded);
    }

    [Fact]
    public void Two_conditions_sharing_a_patient_produce_one_normalized_patient()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-a",
                "condition-a",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-b",
                "condition-b",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Patient patient = Assert.Single(result.Patients);

        Assert.Equal(2, result.PrimaryCancerDiagnoses.Count);
        Assert.All(
            result.PrimaryCancerDiagnoses,
            diagnosis => Assert.Equal(patient.Id, diagnosis.PatientId));
    }
}
