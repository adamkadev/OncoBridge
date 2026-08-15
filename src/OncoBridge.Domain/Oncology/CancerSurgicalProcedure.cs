using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>
/// A cancer-related surgical procedure.
/// </summary>
/// <remarks>
/// Named narrowly on purpose. V1 covers surgical procedures only; medications and systemic therapy
/// are a separate normalisation problem and are out of scope. The narrow name means any drift into
/// that scope shows up in a diff rather than accumulating quietly.
/// </remarks>
public sealed class CancerSurgicalProcedure
{
    /// <summary>Creates a cancer-related surgical procedure.</summary>
    /// <param name="id">This procedure's own identity.</param>
    /// <param name="patientId">The patient this procedure belongs to.</param>
    /// <param name="code">The procedure code as supplied. Required.</param>
    /// <param name="performed">
    /// When the procedure was performed, as a point or an interval, if stated. A procedure stated
    /// as an interval keeps that interval — it is never collapsed to its start.
    /// </param>
    /// <param name="bodySite">Body site as supplied, if stated.</param>
    public CancerSurgicalProcedure(
        Guid id,
        PatientId patientId,
        CodedConcept code,
        TemporalOccurrence? performed = null,
        CodedConcept? bodySite = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        Id = id;
        PatientId = patientId;
        Code = code;
        Performed = performed;
        BodySite = bodySite;
    }

    /// <summary>This procedure's own identity.</summary>
    public Guid Id { get; }

    /// <summary>The patient this procedure belongs to.</summary>
    public PatientId PatientId { get; }

    /// <summary>The procedure code as supplied.</summary>
    public CodedConcept Code { get; }

    /// <summary>When the procedure was performed, if stated. Clinical time.</summary>
    public TemporalOccurrence? Performed { get; }

    /// <summary>Body site as supplied, if stated.</summary>
    public CodedConcept? BodySite { get; }
}
