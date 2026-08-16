using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed class PrimaryCancerDiagnosis
{
    private PrimaryCancerDiagnosis() => Code = null!;

    public PrimaryCancerDiagnosis(
        PrimaryCancerDiagnosisId id,
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

    public PrimaryCancerDiagnosisId Id { get; }

    public PatientId PatientId { get; }

    public CodedConcept Code { get; }

    public TemporalOccurrence? Onset { get; }

    public CodedConcept? BodySite { get; }

    public PartialDate? RecordedDate { get; }
}
