using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Imports;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class RawByteImportTests(ApiPostgreSqlFixture fixture)
{
    private const string WhitespaceHeavyBundle = """
        {
            "resourceType"   :   "Bundle" ,

            "type" : "collection" ,
            "entry"    : [
                {
                    "fullUrl" : "urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa" ,
                    "resource" : {
                        "resourceType" : "Patient" ,
                        "id"           : "patient-001" ,
                        "birthDate"    : "1968"
                    }
                }
            ]
        }
        """;

    [Fact]
    public async Task The_import_content_hash_is_the_hash_of_the_exact_posted_bytes()
    {
        byte[] payload = ApiFixtures.Utf8(WhitespaceHeavyBundle);

        Guid importBatchId = await ApiFixtures.ImportAsync(fixture.Client, payload);

        JsonElement import = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        Assert.Equal(
            ApiFixtures.Sha256Hex(payload),
            import.GetProperty("contentHash").GetString());
    }

    [Fact]
    public void A_reserialized_body_hashes_differently_from_the_posted_body()
    {
        byte[] posted = ApiFixtures.Utf8(WhitespaceHeavyBundle);

        using JsonDocument document = JsonDocument.Parse(posted);
        byte[] reserialized = JsonSerializer.SerializeToUtf8Bytes(document.RootElement);

        Assert.NotEqual(ApiFixtures.Sha256Hex(posted), ApiFixtures.Sha256Hex(reserialized));
    }

    [Fact]
    public async Task The_acceptance_fixture_hashes_to_its_own_bytes_over_HTTP()
    {
        Guid importBatchId =
            await ApiFixtures.ImportAsync(fixture.Client, ApiFixtures.AcceptanceBundleBytes);

        JsonElement import = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        Assert.Equal(
            ApiFixtures.Sha256Hex(ApiFixtures.AcceptanceBundleBytes),
            import.GetProperty("contentHash").GetString());
    }
}
