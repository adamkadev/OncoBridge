using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Imports;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ImportMetadataTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task An_import_without_query_parameters_records_the_default_source_system_label()
    {
        JsonElement import = await ImportAndReadAsync(query: null);

        Assert.Equal("api", import.GetProperty("sourceSystemLabel").GetString());
        Assert.Equal(JsonValueKind.Null, import.GetProperty("fileName").ValueKind);
    }

    [Fact]
    public async Task Supplied_import_metadata_is_recorded()
    {
        JsonElement import =
            await ImportAndReadAsync("sourceSystemLabel=registry-a&fileName=batch-7.json");

        Assert.Equal("registry-a", import.GetProperty("sourceSystemLabel").GetString());
        Assert.Equal("batch-7.json", import.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task A_blank_file_name_is_recorded_as_absent()
    {
        JsonElement import = await ImportAndReadAsync("fileName=%20");

        Assert.Equal(JsonValueKind.Null, import.GetProperty("fileName").ValueKind);
    }

    [Fact]
    public async Task An_import_is_normalized_and_assessed_by_the_time_it_is_readable()
    {
        JsonElement import = await ImportAndReadAsync(query: null);

        Assert.Equal("Normalized", import.GetProperty("status").GetString());
        Assert.Equal("1.0.0", import.GetProperty("normalizerVersion").GetString());
        Assert.NotEqual(JsonValueKind.Null, import.GetProperty("normalizedAt").ValueKind);
        Assert.Equal("collection", import.GetProperty("bundleType").GetString());
    }

    [Fact]
    public async Task Source_resources_are_returned_in_bundle_entry_order()
    {
        JsonElement import = await ImportAndReadAsync(query: null);

        int[] entryIndexes =
        [
            .. import.GetProperty("sourceResources")
                .EnumerateArray()
                .Select(source => source.GetProperty("entryIndex").GetInt32()),
        ];

        Assert.Equal([0, 1, 2, 3, 4, 5, 6], entryIndexes);
    }

    [Fact]
    public async Task The_import_response_never_carries_the_stored_payload_bytes()
    {
        JsonElement import = await ImportAndReadAsync(query: null);

        Assert.DoesNotContain(
            "rawPayload",
            import.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task A_stored_source_resource_is_returned_as_a_JSON_object()
    {
        JsonElement import = await ImportAndReadAsync(query: null);

        JsonElement patient = import.GetProperty("sourceResources")
            .EnumerateArray()
            .Single(source => source.GetProperty("resourceType").GetString() == "Patient");

        JsonElement resourceJson = patient.GetProperty("resourceJson");

        Assert.Equal(JsonValueKind.Object, resourceJson.ValueKind);
        Assert.Equal("Patient", resourceJson.GetProperty("resourceType").GetString());
    }

    private async Task<JsonElement> ImportAndReadAsync(string? query)
    {
        using HttpResponseMessage response = await ApiFixtures.PostBundleAsync(
            fixture.Client, ApiFixtures.AcceptanceBundleBytes, query);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        Guid importBatchId =
            (await ApiFixtures.ReadJsonAsync(response)).GetProperty("importBatchId").GetGuid();

        return await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");
    }
}
