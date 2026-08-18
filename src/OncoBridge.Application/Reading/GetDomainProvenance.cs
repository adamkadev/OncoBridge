using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Reading;

public sealed class GetDomainProvenance(IOncoBridgeReadStore readStore)
{
    public Task<IReadOnlyList<Lineage>> ExecuteAsync(
        Guid domainEntityId, CancellationToken cancellationToken = default) =>
        readStore.GetProvenanceAsync(domainEntityId, cancellationToken);
}
