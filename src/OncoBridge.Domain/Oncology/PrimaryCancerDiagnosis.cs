using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>
/// An asserted primary cancer diagnosis — the anchor concept the rest of the record hangs from.
/// </summary>
/// <remarks>
/// Named for the assertion rather than for its source container. A diagnosis is a specific clinical
/// assertion; the interchange format's container holds many different kinds of thing. Keeping the
/// domain name distinct is part of the boundary this project exists to demonstrate (ADR-0001).
/// </remarks>
public sealed class PrimaryCancerDiagnosis
{
    /// <summary>Creates a primary cancer diagnosis.</summary>
    /// <param name="id">This diagnosis's own identity.</param>
    /// <param name="patientId">The patient this diagnosis belongs to.</param>
    /// <param name="code">The diagnosis code as supplied. Required — a diagnosis without a code asserts nothing.</param>
    /// <param name="onset">When onset occurred, as a point or an interval, if stated.</param>
    /// <param name="bodySite">Body site as supplied, if stated.</param>
    /// <param name="recordedDate">
    /// When the source system recorded the assertion. This is authoring time, not clinical time,
    /// and must never be used for clinical ordering.
    /// </param>
    public PrimaryCancerDiagnosis(
        Guid id,
        PatientId patientId,
        CodedConcept code,
        TemporalOccurrence? onset = null,
        CodedConcept? bodySite = null,
        PartialDate? recordedDate = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        Id = id;
        PatientId = patientId;
        Code = code;
        Onset = onset;
        BodySite = bodySite;
        RecordedDate = recordedDate;
    }

    /// <summary>This diagnosis's own identity.</summary>
    public Guid Id { get; }

    /// <summary>The patient this diagnosis belongs to.</summary>
    public PatientId PatientId { get; }

    /// <summary>The diagnosis code as supplied.</summary>
    public CodedConcept Code { get; }

    /// <summary>When onset occurred, if the source stated it.</summary>
    public TemporalOccurrence? Onset { get; }

    /// <summary>Body site as supplied, if stated.</summary>
    public CodedConcept? BodySite { get; }

    /// <summary>When the source system recorded the assertion — authoring time, not clinical time.</summary>
    public PartialDate? RecordedDate { get; }
}
