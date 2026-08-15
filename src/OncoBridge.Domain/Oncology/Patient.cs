using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed class Patient
{
    public Patient(
        PatientId id,
        string? sourceIdentifier = null,
        PartialDate? birthDate = null,
        CodedConcept? sexAtBirthAsRecorded = null)
    {
        Id = id;
        SourceIdentifier = sourceIdentifier;
        BirthDate = birthDate;
        SexAtBirthAsRecorded = sexAtBirthAsRecorded;
    }

    public PatientId Id { get; }

    public string? SourceIdentifier { get; }

    public PartialDate? BirthDate { get; }

    public CodedConcept? SexAtBirthAsRecorded { get; }
}
