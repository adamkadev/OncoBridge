using Microsoft.AspNetCore.Mvc;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Mapping;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Api.Endpoints;

internal static class ProvenanceEndpoints
{
    private const string ProvenanceTag = "Provenance";

    internal static RouteGroupBuilder MapProvenanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/domain/{domainEntityId:guid}/provenance", GetDomainProvenanceAsync)
            .WithName("GetDomainProvenance")
            .WithTags(ProvenanceTag)
            .WithSummary("Read the lineage of a canonical entity")
            .WithDescription(
                "Returns the lineage records that name this canonical entity: the whole-entity record "
                + "first, then one record per derived field. Each names the source resource it was "
                + "read from and the transformation that produced it.")
            .Produces<ProvenanceResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetDomainProvenanceAsync(
        Guid domainEntityId,
        [FromServices] GetDomainProvenance getDomainProvenance,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Lineage> records =
            await getDomainProvenance.ExecuteAsync(domainEntityId, cancellationToken);

        return records.Count == 0
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProvenanceMapping.ToResponse(domainEntityId, records));
    }
}
