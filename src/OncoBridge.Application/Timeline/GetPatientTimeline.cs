using OncoBridge.Application.Reading;
using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Timeline;

public sealed class GetPatientTimeline(IOncoBridgeReadStore readStore)
{
    public async Task<PatientTimeline?> ExecuteAsync(
        PatientId patientId, CancellationToken cancellationToken = default)
    {
        PatientRecord? record = await readStore.GetPatientRecordAsync(patientId, cancellationToken);

        return record is null ? null : PatientTimelineProjector.Project(record);
    }
}
