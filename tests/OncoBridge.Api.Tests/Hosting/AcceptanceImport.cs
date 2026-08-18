using System.Text.Json;

namespace OncoBridge.Api.Tests.Hosting;

internal sealed record AcceptanceImport(Guid ImportBatchId, JsonElement Import, Guid PatientId)
{
    internal static async Task<AcceptanceImport> RunAsync(HttpClient client)
    {
        Guid importBatchId = await ApiFixtures.ImportAsync(client, ApiFixtures.AcceptanceBundleBytes);
        JsonElement import =
            await ApiFixtures.GetJsonAsync(client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        return new AcceptanceImport(importBatchId, import, SourceIdOf(import, "patient-001"));
    }

    internal Task<JsonElement> RecordAsync(HttpClient client) =>
        ApiFixtures.GetJsonAsync(client, $"/api/v1/patients/{PatientId}/record");

    internal Task<JsonElement> TimelineAsync(HttpClient client) =>
        ApiFixtures.GetJsonAsync(client, $"/api/v1/patients/{PatientId}/timeline");

    internal Task<JsonElement> FindingsAsync(HttpClient client) =>
        ApiFixtures.GetJsonAsync(client, $"{ApiFixtures.ImportsRoute}/{ImportBatchId}/findings");

    internal Guid SourceId(string sourceLogicalId) => SourceIdOf(Import, sourceLogicalId);

    private static Guid SourceIdOf(JsonElement import, string sourceLogicalId) =>
        import.GetProperty("sourceResources")
            .EnumerateArray()
            .Single(source => source.GetProperty("sourceLogicalId").GetString() == sourceLogicalId)
            .GetProperty("id")
            .GetGuid();
}
