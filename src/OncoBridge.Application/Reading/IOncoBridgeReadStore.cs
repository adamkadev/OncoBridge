using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Application.Reading;

public interface IOncoBridgeReadStore
{
    Task<ImportDetails?> GetImportAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default);

    Task<PatientRecord?> GetPatientRecordAsync(
        PatientId patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Finding>?> GetFindingsAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lineage>> GetProvenanceAsync(
        Guid domainEntityId, CancellationToken cancellationToken = default);
}
