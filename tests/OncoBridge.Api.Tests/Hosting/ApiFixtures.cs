using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OncoBridge.Api.Tests.Hosting;

internal static class ApiFixtures
{
    internal const string ImportsRoute = "/api/v1/imports";

    internal const string FhirJsonMediaType = "application/fhir+json";

    private static readonly string RepoRoot = ResolveRepoRoot();

    internal static byte[] AcceptanceBundleBytes { get; } =
        File.ReadAllBytes(BundlePath("phase4/bundle-acceptance-defects"));

    internal static byte[] StructuralMalformedBundleBytes { get; } =
        File.ReadAllBytes(BundlePath("phase4/bundle-structural-malformed"));

    internal static string OpenApiSnapshot { get; } = File.ReadAllText(
        Path.Combine(RepoRoot, "tests/OncoBridge.Api.Tests/Snapshots/openapi-v1.json"));

    internal static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    internal static string Sha256Hex(byte[] payload) =>
        Convert.ToHexStringLower(SHA256.HashData(payload));

    internal static Task<HttpResponseMessage> PostBundleAsync(
        HttpClient client, byte[] payload, string? query = null) =>
        PostAsync(client, payload, FhirJsonMediaType, query);

    internal static Task<HttpResponseMessage> PostAsync(
        HttpClient client, byte[] payload, string mediaType, string? query = null)
    {
        ByteArrayContent content = new(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

        return client.PostAsync(query is null ? ImportsRoute : $"{ImportsRoute}?{query}", content);
    }

    internal static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using JsonDocument document =
            JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    internal static async Task<Guid> ImportAsync(HttpClient client, byte[] payload)
    {
        using HttpResponseMessage response = await PostBundleAsync(client, payload);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        return (await ReadJsonAsync(response)).GetProperty("importBatchId").GetGuid();
    }

    internal static async Task<JsonElement> GetJsonAsync(HttpClient client, string route)
    {
        using HttpResponseMessage response = await client.GetAsync(route);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        return await ReadJsonAsync(response);
    }

    private static string BundlePath(string name) =>
        Path.Combine(RepoRoot, $"test-data/synthetic/{name}.json");

    private static string ResolveRepoRoot()
    {
        string? value = typeof(ApiFixtures).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "RepoRoot")
            ?.Value;

        return Path.GetFullPath(
            value ?? throw new InvalidOperationException("RepoRoot is not configured."));
    }
}
