using System.Net;
using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Imports;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class InvalidImportTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task An_unparseable_JSON_body_is_rejected()
    {
        using HttpResponseMessage response =
            await ApiFixtures.PostBundleAsync(fixture.Client, ApiFixtures.Utf8("""{"resourceType":"""));

        await AssertImportFailedAsync(response);
    }

    [Fact]
    public async Task A_JSON_object_that_is_not_a_FHIR_Bundle_is_rejected()
    {
        using HttpResponseMessage response = await ApiFixtures.PostBundleAsync(
            fixture.Client, ApiFixtures.Utf8("""{"resourceType":"Patient","id":"patient-001"}"""));

        await AssertImportFailedAsync(response);
    }

    [Fact]
    public async Task An_empty_body_is_rejected()
    {
        using HttpResponseMessage response = await ApiFixtures.PostBundleAsync(fixture.Client, []);

        await AssertImportFailedAsync(response);
    }

    [Fact]
    public async Task A_non_JSON_content_type_is_refused_before_the_body_is_read()
    {
        using HttpResponseMessage response = await ApiFixtures.PostAsync(
            fixture.Client, ApiFixtures.AcceptanceBundleBytes, "text/plain");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Plain_application_json_is_accepted_as_well_as_the_FHIR_media_type()
    {
        using HttpResponseMessage response = await ApiFixtures.PostAsync(
            fixture.Client, ApiFixtures.AcceptanceBundleBytes, "application/json");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_blank_source_system_label_is_refused()
    {
        using HttpResponseMessage response = await ApiFixtures.PostBundleAsync(
            fixture.Client, ApiFixtures.AcceptanceBundleBytes, "sourceSystemLabel=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_valid_bundle_carrying_an_unreadable_resource_still_imports()
    {
        Guid importBatchId = await ApiFixtures.ImportAsync(
            fixture.Client, ApiFixtures.StructuralMalformedBundleBytes);

        JsonElement import = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        JsonElement malformed = import.GetProperty("sourceResources")
            .EnumerateArray()
            .Single(source => source.GetProperty("resourceType").GetString() == "NotAKnownFhirResource");

        Assert.NotEqual(JsonValueKind.Null, malformed.GetProperty("resourceJson").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(malformed.GetProperty("contentHash").GetString()));

        JsonElement findings = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}/findings");

        JsonElement structural = Assert.Single(
            findings.EnumerateArray(),
            finding => finding.GetProperty("checkId").GetString() == "OB-STR-001");

        Assert.Equal(
            malformed.GetProperty("id").GetGuid(),
            structural.GetProperty("target").GetProperty("id").GetGuid());
    }

    private static async Task AssertImportFailedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement problem = await ApiFixtures.ReadJsonAsync(response);

        Assert.Equal("FHIR Bundle import failed", problem.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.DoesNotContain("   at ", problem.GetProperty("detail").GetString()!);
    }
}
