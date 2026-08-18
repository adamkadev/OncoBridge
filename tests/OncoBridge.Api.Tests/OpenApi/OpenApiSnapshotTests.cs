using System.Net;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.OpenApi;

public sealed class OpenApiSnapshotTests
{
    private const string DocumentRoute = "/openapi/v1.json";

    [Fact]
    public async Task The_generated_document_matches_the_committed_snapshot()
    {
        string actual = OpenApiCanonicalJson.Canonicalize(await FetchDocumentAsync());
        string snapshot = OpenApiCanonicalJson.Canonicalize(ApiFixtures.OpenApiSnapshot);

        Assert.Equal(snapshot, actual);
    }

    [Fact]
    public async Task The_document_is_served_as_JSON_without_opening_a_database_connection()
    {
        await using OncoBridgeApiFactory factory = OncoBridgeApiFactory.WithoutDatabase();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(DocumentRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void Reordered_properties_and_whitespace_do_not_break_the_comparison()
    {
        const string Compact = """{"b":[2,1],"a":{"d":1,"c":2}}""";
        const string Expanded = """
            {
              "a": { "c": 2, "d": 1 },
              "b": [ 1, 2 ]
            }
            """;

        Assert.Equal(
            OpenApiCanonicalJson.Canonicalize(Compact),
            OpenApiCanonicalJson.Canonicalize(Expanded));
    }

    [Fact]
    public void A_dropped_required_field_breaks_the_comparison() =>
        Assert.NotEqual(
            OpenApiCanonicalJson.Canonicalize("""{"required":["a","b"]}"""),
            OpenApiCanonicalJson.Canonicalize("""{"required":["a"]}"""));

    private static async Task<string> FetchDocumentAsync()
    {
        await using OncoBridgeApiFactory factory = OncoBridgeApiFactory.WithoutDatabase();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(DocumentRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadAsStringAsync();
    }
}
