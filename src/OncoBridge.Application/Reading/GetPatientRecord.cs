using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Reading;

public sealed class GetPatientRecord(IOncoBridgeReadStore readStore)
{
    public Task<PatientRecord?> ExecuteAsync(
        PatientId patientId, CancellationToken cancellationToken = default) =>
        readStore.GetPatientRecordAsync(patientId, cancellationToken);
}
