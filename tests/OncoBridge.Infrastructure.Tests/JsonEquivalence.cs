using System.Text.Json;
using System.Text.Json.Nodes;

namespace OncoBridge.Infrastructure.Tests;

internal static class JsonEquivalence
{
    internal static void AssertEquivalent(string expected, string actual) =>
        Assert.Equal(Canonicalise(expected), Canonicalise(actual));

    internal static bool AreEquivalent(string expected, string actual) =>
        Canonicalise(expected) == Canonicalise(actual);

    private static string Canonicalise(string json) =>
        Canonicalise(JsonNode.Parse(json))?.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
            ?? "null";

    private static JsonNode? Canonicalise(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(
            obj.OrderBy(pair => pair.Key, StringComparer.Ordinal)
               .Select(pair => KeyValuePair.Create(pair.Key, Canonicalise(pair.Value?.DeepClone())))),

        JsonArray array => new JsonArray([.. array.Select(item => Canonicalise(item?.DeepClone()))]),

        _ => node,
    };
}
