using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed class CancerSurgicalProcedure
{
    private CancerSurgicalProcedure() => Code = null!;

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

    public Guid Id { get; }

    public PatientId PatientId { get; }

    public CodedConcept Code { get; }

    public TemporalOccurrence? Performed { get; }

    public CodedConcept? BodySite { get; }
}
