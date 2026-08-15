using Hl7.Fhir.Model;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class FhirCodedConcepts
{
    internal static CodedConcept? FromFirstUsableCoding(CodeableConcept? concept)
    {
        if (concept?.Coding is null)
        {
            return null;
        }

        foreach (Coding coding in concept.Coding)
        {
            if (!string.IsNullOrWhiteSpace(coding.System) && !string.IsNullOrWhiteSpace(coding.Code))
            {
                return new CodedConcept(coding.System, coding.Code, coding.Display);
            }
        }

        return null;
    }

    internal static CodedConcept? FromFirstUsableCoding(IReadOnlyList<CodeableConcept>? concepts)
    {
        if (concepts is null)
        {
            return null;
        }

        foreach (CodeableConcept concept in concepts)
        {
            if (FromFirstUsableCoding(concept) is { } coded)
            {
                return coded;
            }
        }

        return null;
    }
}
