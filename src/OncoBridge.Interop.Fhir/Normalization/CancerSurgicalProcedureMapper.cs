using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using FhirProcedure = Hl7.Fhir.Model.Procedure;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class CancerSurgicalProcedureMapper
{
    internal static CancerSurgicalProcedure? ToSurgicalProcedure(
        FhirProcedure source, SourceResourceId sourceResourceId, PatientId patientId)
    {
        if (FhirCodedConcepts.FromFirstUsableCoding(source.Code) is not { } code)
        {
            return null;
        }

        return new CancerSurgicalProcedure(
            sourceResourceId.Value,
            patientId,
            code,
            FhirTemporalMapper.ToOccurrence(source.Performed),
            FhirCodedConcepts.FromFirstUsableCoding(source.BodySite));
    }
}
