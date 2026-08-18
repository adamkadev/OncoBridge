using System.Text.Json;
using OncoBridge.Api.Contracts;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Api.Mapping;

internal static class ImportMapping
{
    internal static ImportResponse ToResponse(ImportDetails details) => new()
    {
        ImportBatchId = details.Import.Id.Value,
        SourceSystemLabel = details.Import.SourceSystemLabel,
        ReceivedAt = details.Import.ReceivedAt,
        FileName = details.Import.FileName,
        ContentHash = details.Import.ContentHash.Value,
        BundleType = details.Import.BundleType,
        EntryCount = details.Import.EntryCount,
        Status = details.Import.Status.ToString(),
        NormalizerVersion = details.Import.NormalizerVersion,
        NormalizedAt = details.Import.NormalizedAt,
        SourceResources = [.. details.SourceResources.Select(ToResponse)],
    };

    private static SourceResourceResponse ToResponse(SourceResource resource) => new()
    {
        Id = resource.Id.Value,
        EntryIndex = resource.EntryIndex,
        ResourceType = resource.ResourceType,
        SourceLogicalId = resource.SourceLogicalId,
        FullUrl = resource.FullUrl,
        ContentHash = resource.ContentHash?.Value,
        ResourceJson = ToJsonValue(resource.ResourceJson),
    };

    private static JsonElement? ToJsonValue(string? resourceJson)
    {
        if (resourceJson is null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(resourceJson);

        return document.RootElement.Clone();
    }
}
