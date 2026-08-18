using System.Text;
using System.Text.Json;

namespace OncoBridge.Api.Tests.OpenApi;

internal static class OpenApiCanonicalJson
{
    private const string ServersProperty = "servers";

    internal static string Canonicalize(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        StringBuilder builder = new();

        Write(document.RootElement, builder, isDocumentRoot: true);

        return builder.ToString();
    }

    private static void Write(JsonElement element, StringBuilder builder, bool isDocumentRoot = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(element, builder, isDocumentRoot);
                break;

            case JsonValueKind.Array:
                WriteArray(element, builder);
                break;

            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    private static void WriteObject(JsonElement element, StringBuilder builder, bool isDocumentRoot)
    {
        IEnumerable<JsonProperty> properties = element
            .EnumerateObject()
            .Where(property => !(isDocumentRoot && property.NameEquals(ServersProperty)))
            .OrderBy(property => property.Name, StringComparer.Ordinal);

        builder.Append('{');
        bool first = true;

        foreach (JsonProperty property in properties)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            builder.Append(JsonSerializer.Serialize(property.Name)).Append(':');
            Write(property.Value, builder);
        }

        builder.Append('}');
    }

    private static void WriteArray(JsonElement element, StringBuilder builder)
    {
        List<string> items = [];

        foreach (JsonElement item in element.EnumerateArray())
        {
            StringBuilder itemBuilder = new();
            Write(item, itemBuilder);
            items.Add(itemBuilder.ToString());
        }

        items.Sort(StringComparer.Ordinal);

        builder.Append('[').AppendJoin(',', items).Append(']');
    }
}
