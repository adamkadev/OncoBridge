using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed class FhirBundleExtractor
{
    private readonly BundleIngestionOptions _options;
    private readonly FhirJsonDeserializer _deserializer = new();

    public FhirBundleExtractor(BundleIngestionOptions? options = null) =>
        _options = options ?? BundleIngestionOptions.Default;

    public ExtractedBundle Extract(ReadOnlyMemory<byte> payload)
    {
        if (payload.IsEmpty)
        {
            throw new BundleIngestionException("Payload is empty.");
        }

        if (payload.Length > _options.MaxPayloadBytes)
        {
            throw new BundleIngestionException(
                $"Payload is {payload.Length} bytes, exceeding the {_options.MaxPayloadBytes} byte limit.");
        }

        using JsonDocument document = ParseEnvelope(payload);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new BundleIngestionException("Payload root is not a JSON object.");
        }

        if (ReadString(root, FhirJsonElements.ResourceType) != FhirResourceTypes.Bundle)
        {
            throw new BundleIngestionException("Payload is not a FHIR Bundle.");
        }

        return new ExtractedBundle
        {
            BundleType = ReadString(root, FhirJsonElements.BundleType),
            Entries = ExtractEntries(root),
        };
    }

    private static JsonDocument ParseEnvelope(ReadOnlyMemory<byte> payload)
    {
        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException exception)
        {
            throw new BundleIngestionException("Payload is not valid JSON.", exception);
        }
    }

    private IReadOnlyList<ExtractedEntry> ExtractEntries(JsonElement root)
    {
        if (!root.TryGetProperty(FhirJsonElements.BundleEntry, out JsonElement entries))
        {
            return [];
        }

        if (entries.ValueKind != JsonValueKind.Array)
        {
            throw new BundleIngestionException("Bundle.entry is present but is not an array.");
        }

        int declaredCount = entries.GetArrayLength();
        if (declaredCount > _options.MaxEntryCount)
        {
            throw new BundleIngestionException(
                $"Bundle declares {declaredCount} entries, exceeding the {_options.MaxEntryCount} entry limit.");
        }

        List<ExtractedEntry> extracted = new(declaredCount);
        int index = 0;

        foreach (JsonElement entry in entries.EnumerateArray())
        {
            extracted.Add(ExtractEntry(entry, index));
            index++;
        }

        return extracted;
    }

    private ExtractedEntry ExtractEntry(JsonElement entry, int entryIndex)
    {
        string? fullUrl = entry.ValueKind == JsonValueKind.Object ? ReadString(entry, FhirJsonElements.EntryFullUrl) : null;

        if (entry.ValueKind != JsonValueKind.Object
            || !entry.TryGetProperty(FhirJsonElements.EntryResource, out JsonElement resource)
            || resource.ValueKind != JsonValueKind.Object)
        {
            return new ExtractedEntry
            {
                EntryIndex = entryIndex,
                RawResourceJson = ReadOnlyMemory<byte>.Empty,
                FullUrl = fullUrl,
            };
        }

        byte[] rawResource = Encoding.UTF8.GetBytes(resource.GetRawText());

        return Interpret(rawResource, entryIndex, fullUrl, ReadString(resource, FhirJsonElements.ResourceType));
    }

    private ExtractedEntry Interpret(
        byte[] rawResource, int entryIndex, string? fullUrl, string? declaredResourceType)
    {
        try
        {
            Utf8JsonReader reader = new(rawResource);

            if (_deserializer.TryDeserializeResource(ref reader, out Resource? resource, out var issues)
                && resource is not null)
            {
                return new ExtractedEntry
                {
                    EntryIndex = entryIndex,
                    RawResourceJson = rawResource,
                    FullUrl = fullUrl,
                    ResourceType = resource.TypeName,
                    SourceLogicalId = resource.Id,
                };
            }

            return Uninterpretable(Describe(issues));
        }
        catch (DeserializationFailedException exception)
        {
            return Uninterpretable(exception.GetType().Name);
        }
        catch (JsonException exception)
        {
            return Uninterpretable(exception.GetType().Name);
        }

        ExtractedEntry Uninterpretable(string reason) => new()
        {
            EntryIndex = entryIndex,
            RawResourceJson = rawResource,
            FullUrl = fullUrl,
            ResourceType = declaredResourceType,
            InterpretationError = reason,
        };
    }

    private static string Describe(IEnumerable<Exception>? issues)
    {
        if (issues is null)
        {
            return "Resource could not be read as FHIR R4.";
        }

        string[] codes = [.. issues.Select(issue => issue.GetType().Name).Distinct().Take(5)];

        return codes.Length == 0
            ? "Resource could not be read as FHIR R4."
            : $"Resource could not be read as FHIR R4: {string.Join(", ", codes)}.";
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
