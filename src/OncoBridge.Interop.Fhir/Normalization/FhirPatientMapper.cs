using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using FhirPatient = Hl7.Fhir.Model.Patient;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class FhirPatientMapper
{
    internal static Patient ToPatient(FhirPatient source, PatientId id) => new(
        id,
        FirstUsableIdentifier(source),
        FhirTemporalMapper.ToPartialDate(source.BirthDate));

    private static string? FirstUsableIdentifier(FhirPatient source) =>
        source.Identifier?
            .Select(identifier => identifier.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
