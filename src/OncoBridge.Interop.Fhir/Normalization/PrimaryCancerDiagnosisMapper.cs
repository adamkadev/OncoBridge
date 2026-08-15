using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Terminology;
using FhirCondition = Hl7.Fhir.Model.Condition;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class PrimaryCancerDiagnosisMapper
{
    internal static PrimaryCancerDiagnosis? ToDiagnosis(
        FhirCondition source, SourceResourceId sourceResourceId, PatientId patientId)
    {
        if (FhirCodedConcepts.FromFirstUsableCoding(source.Code) is not { } code)
        {
            return null;
        }

        return new PrimaryCancerDiagnosis(
            sourceResourceId.Value,
            patientId,
            code,
            FhirTemporalMapper.ToOccurrence(source.Onset),
            FhirCodedConcepts.FromFirstUsableCoding(source.BodySite),
            FhirTemporalMapper.ToPartialDate(source.RecordedDate));
    }
}
