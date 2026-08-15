using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>
/// The subject of care, held deliberately shallow.
/// </summary>
/// <remarks>
/// V1 records only what the selected concepts actually need: an identifier as supplied, a birth
/// date, and sex at birth as it was recorded. Nothing else is modelled, because nothing else is
/// used — this is plumbing rather than a showcase, and growing it would be scope creep.
/// </remarks>
public sealed class Patient
{
    /// <summary>Creates a patient.</summary>
    /// <param name="id">The OncoBridge identity for this patient.</param>
    /// <param name="sourceIdentifier">The identifier as supplied by the source, if any.</param>
    /// <param name="birthDate">Birth date at whatever precision the source stated.</param>
    /// <param name="sexAtBirthAsRecorded">
    /// Sex at birth exactly as the source coded it. The name says "as recorded" because OncoBridge
    /// carries the value through without interpreting or re-coding it.
    /// </param>
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

    /// <summary>The OncoBridge identity for this patient.</summary>
    public PatientId Id { get; }

    /// <summary>The identifier as supplied by the source, if any.</summary>
    public string? SourceIdentifier { get; }

    /// <summary>Birth date at the precision the source stated it.</summary>
    public PartialDate? BirthDate { get; }

    /// <summary>Sex at birth exactly as the source coded it.</summary>
    public CodedConcept? SexAtBirthAsRecorded { get; }
}
