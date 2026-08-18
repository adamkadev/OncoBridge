using OncoBridge.Api.Contracts;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Api.Mapping;

internal static class ProvenanceMapping
{
    internal static ProvenanceResponse ToResponse(
        Guid domainEntityId, IReadOnlyList<Lineage> records) => new()
    {
        DomainEntityId = domainEntityId,
        Records = [.. records.Select(ToResponse)],
    };

    private static LineageResponse ToResponse(Lineage lineage) => new()
    {
        DomainEntityType = lineage.DomainEntityType,
        DomainEntityId = lineage.DomainEntityId,
        FieldPath = lineage.FieldPath,
        SourceResourceId = lineage.SourceResourceId.Value,
        TransformationName = lineage.TransformationName,
        TransformationVersion = lineage.TransformationVersion,
    };
}
