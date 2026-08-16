using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Interop.Fhir.Normalization;

internal sealed class FhirResourceReader
{
    private readonly FhirJsonDeserializer _deserializer = new();

    internal T? Read<T>(SourceResource source)
        where T : Resource => Read(source) as T;

    internal Resource? Read(SourceResource source)
    {
        if (string.IsNullOrWhiteSpace(source.ResourceJson))
        {
            return null;
        }

        try
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(source.ResourceJson));

            return _deserializer.TryDeserializeResource(ref reader, out Resource? resource, out _)
                ? resource
                : null;
        }
        catch (DeserializationFailedException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
