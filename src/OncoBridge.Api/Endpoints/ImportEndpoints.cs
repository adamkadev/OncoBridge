using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Mapping;
using OncoBridge.Application.Imports;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Api.Endpoints;

internal static class ImportEndpointDefaults
{
    internal const string SourceSystemLabel = "api";

    internal const string FhirJsonMediaType = "application/fhir+json";
}

internal static class ImportEndpoints
{
    private const string ImportsTag = "Imports";

    private const string QualityTag = "Quality";

    private const string JsonStructuredSyntaxSuffix = "json";

    internal static RouteGroupBuilder MapImportEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/imports", ImportAsync)
            .WithName("ImportBundle")
            .WithTags(ImportsTag)
            .WithSummary("Import a FHIR R4 Bundle")
            .WithDescription(
                "Preserves the request body byte for byte as import evidence, then normalizes the "
                + "batch into the canonical oncology tier and assesses its quality. The batch is "
                + "queryable through the read endpoints as soon as this call returns.")
            .Accepts<JsonElement>(
                ImportEndpointDefaults.FhirJsonMediaType, MediaTypeNames.Application.Json)
            .Produces<ImportCreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapGet("/imports/{id:guid}", GetImportAsync)
            .WithName("GetImport")
            .WithTags(ImportsTag)
            .WithSummary("Read an import batch with its source resources")
            .WithDescription(
                "Returns the import metadata and every stored source resource in bundle entry order. "
                + "contentHash is the SHA-256 of the exact posted bytes; the posted bytes themselves "
                + "are retained in storage and are not served by this API.")
            .Produces<ImportResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/imports/{id:guid}/findings", GetImportFindingsAsync)
            .WithName("GetImportFindings")
            .WithTags(QualityTag)
            .WithSummary("Read the quality findings of an import batch")
            .WithDescription(
                "Returns the findings the OncoBridge conformance checks raised about this batch. An "
                + "existing batch with nothing to report returns an empty array; findings of any "
                + "severity are reported inside a successful response.")
            .Produces<IReadOnlyList<FindingResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ImportAsync(
        HttpRequest request,
        [FromServices] ImportPayload importPayload,
        string? sourceSystemLabel,
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (!IsJsonMediaType(request.ContentType))
        {
            return TypedResults.Problem(
                title: "Unsupported media type",
                detail: $"The request body must be JSON; '{request.ContentType}' is not. Post a FHIR "
                    + $"Bundle as '{ImportEndpointDefaults.FhirJsonMediaType}' or "
                    + $"'{MediaTypeNames.Application.Json}'.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (sourceSystemLabel is not null && string.IsNullOrWhiteSpace(sourceSystemLabel))
        {
            return TypedResults.Problem(
                title: "Invalid import metadata",
                detail: "sourceSystemLabel was supplied but is blank. Omit it to record the default "
                    + $"label '{ImportEndpointDefaults.SourceSystemLabel}', or state a real label.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        byte[] payload = await ReadPayloadAsync(request, cancellationToken);

        try
        {
            ImportBatchId batchId = await importPayload.ExecuteAsync(
                payload,
                sourceSystemLabel ?? ImportEndpointDefaults.SourceSystemLabel,
                string.IsNullOrWhiteSpace(fileName) ? null : fileName,
                cancellationToken);

            return TypedResults.Created(
                $"{ApiMetadata.RoutePrefix}/imports/{batchId.Value}",
                new ImportCreatedResponse { ImportBatchId = batchId.Value });
        }
        catch (BundleIngestionException exception)
        {
            return TypedResults.Problem(
                title: "FHIR Bundle import failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetImportAsync(
        Guid id, [FromServices] GetImport getImport, CancellationToken cancellationToken)
    {
        ImportDetails? details = await getImport.ExecuteAsync(new ImportBatchId(id), cancellationToken);

        return details is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ImportMapping.ToResponse(details));
    }

    private static async Task<IResult> GetImportFindingsAsync(
        Guid id, [FromServices] GetImportFindings getFindings, CancellationToken cancellationToken)
    {
        IReadOnlyList<Finding>? findings =
            await getFindings.ExecuteAsync(new ImportBatchId(id), cancellationToken);

        return findings is null
            ? TypedResults.NotFound()
            : TypedResults.Ok<IReadOnlyList<FindingResponse>>(
                [.. findings.Select(QualityMapping.ToResponse)]);
    }

    private static async Task<byte[]> ReadPayloadAsync(
        HttpRequest request, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();

        await request.Body.CopyToAsync(buffer, cancellationToken);

        return buffer.ToArray();
    }

    private static bool IsJsonMediaType(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? mediaType)
        && (mediaType.MatchesMediaType(MediaTypeNames.Application.Json)
            || mediaType.Suffix.Equals(JsonStructuredSyntaxSuffix, StringComparison.OrdinalIgnoreCase));
}
