using System.Net;
using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Imports;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class BoundedRequestBodyTests(ApiPostgreSqlFixture fixture)
{
    private const int Limit = ApiPostgreSqlFixture.BoundedMaxPayloadBytes;

    private const long ForgedContentLength = 10;

    private const string BundleWithoutClosingBrace = """
        {"resourceType":"Bundle","type":"collection","entry":[{"fullUrl":"urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa","resource":{"resourceType":"Patient","id":"patient-001","birthDate":"1968"}}]
        """;

    [Fact]
    public async Task A_payload_of_exactly_the_limit_reaches_ingestion()
    {
        using HttpResponseMessage response =
            await ApiFixtures.PostBundleAsync(fixture.BoundedClient, BundleOfExactly(Limit));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task An_accepted_payload_is_stored_byte_for_byte()
    {
        byte[] payload = BundleOfExactly(Limit - 1);

        using HttpResponseMessage response =
            await ApiFixtures.PostBundleAsync(fixture.BoundedClient, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Guid importBatchId =
            (await ApiFixtures.ReadJsonAsync(response)).GetProperty("importBatchId").GetGuid();

        JsonElement import = await ApiFixtures.GetJsonAsync(
            fixture.BoundedClient, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        Assert.Equal(
            ApiFixtures.Sha256Hex(payload),
            import.GetProperty("contentHash").GetString());
    }

    [Fact]
    public async Task A_payload_above_the_limit_is_refused_on_its_declared_content_length()
    {
        using HttpResponseMessage response =
            await ApiFixtures.PostBundleAsync(fixture.BoundedClient, BundleOfExactly(Limit + 1));

        await AssertPayloadTooLargeAsync(response);
    }

    [Fact]
    public async Task A_payload_above_the_limit_is_refused_while_streaming_a_forged_content_length()
    {
        byte[] payload = BundleOfExactly(Limit * 4);

        using HttpResponseMessage response = await ApiFixtures.PostBundleWithForgedContentLengthAsync(
            fixture.BoundedClient, payload, ForgedContentLength);

        Assert.Equal(
            ForgedContentLength, response.RequestMessage?.Content?.Headers.ContentLength);

        await AssertPayloadTooLargeAsync(response);
    }

    [Fact]
    public async Task An_oversized_request_persists_no_import_batch()
    {
        int before = await fixture.CountImportBatchesAsync();

        using HttpResponseMessage declared =
            await ApiFixtures.PostBundleAsync(fixture.BoundedClient, BundleOfExactly(Limit + 1));
        using HttpResponseMessage streamed =
            await ApiFixtures.PostBundleWithForgedContentLengthAsync(
                fixture.BoundedClient, BundleOfExactly(Limit * 4), ForgedContentLength);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, declared.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, streamed.StatusCode);
        Assert.Equal(before, await fixture.CountImportBatchesAsync());
    }

    [Fact]
    public async Task The_acceptance_bundle_still_imports_under_the_shipped_limit()
    {
        using HttpResponseMessage response = await ApiFixtures.PostBundleAsync(
            fixture.Client, ApiFixtures.AcceptanceBundleBytes);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertPayloadTooLargeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement problem = await ApiFixtures.ReadJsonAsync(response);

        Assert.Equal("Payload too large", problem.GetProperty("title").GetString());
        Assert.Equal(
            $"The request body exceeds the {Limit} byte import limit and was not read into memory.",
            problem.GetProperty("detail").GetString());
        Assert.DoesNotContain("   at ", problem.GetProperty("detail").GetString()!);
    }

    private static byte[] BundleOfExactly(int totalBytes)
    {
        string prefix = BundleWithoutClosingBrace.Trim();
        int padding = totalBytes - prefix.Length - 1;

        Assert.True(padding >= 0, $"{totalBytes} bytes cannot hold the minimal bundle.");

        return ApiFixtures.Utf8($"{prefix}{new string(' ', padding)}}}");
    }
}
