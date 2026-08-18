using System.Net;
using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Acceptance;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class Phase0AcceptanceTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task The_acceptance_bundle_travels_the_whole_pipeline_over_HTTP()
    {
        HttpClient client = fixture.Client;

        Guid importBatchId = await PostAcceptanceBundleAsync(client);

        JsonElement import =
            await ApiFixtures.GetJsonAsync(client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        Assert.Equal(7, import.GetProperty("entryCount").GetInt32());

        JsonElement[] sourceResources = [.. import.GetProperty("sourceResources").EnumerateArray()];

        Assert.Equal(7, sourceResources.Length);
        Assert.All(
            sourceResources,
            source => Assert.False(
                string.IsNullOrWhiteSpace(source.GetProperty("contentHash").GetString())));

        Guid patientId = SourceIdOf(sourceResources, "patient-001");

        JsonElement record = await ApiFixtures.GetJsonAsync(client, $"/api/v1/patients/{patientId}/record");

        Assert.Equal(patientId, record.GetProperty("patient").GetProperty("id").GetGuid());
        Assert.Single(record.GetProperty("primaryCancerDiagnoses").EnumerateArray());
        Assert.Single(record.GetProperty("cancerSurgicalProcedures").EnumerateArray());

        JsonElement staging = Assert.Single(record.GetProperty("cancerStagings").EnumerateArray());

        Assert.Equal(
            ["T", "N", "M"],
            staging.GetProperty("categories")
                .EnumerateArray()
                .Select(category => category.GetProperty("axis").GetString()));

        Guid stagingId = staging.GetProperty("id").GetGuid();

        JsonElement findings = await ApiFixtures.GetJsonAsync(
            client, $"{ApiFixtures.ImportsRoute}/{importBatchId}/findings");

        JsonElement[] reported = [.. findings.EnumerateArray()];

        Assert.Equal(3, reported.Length);
        Assert.Equal(
            ["OB-CONF-001", "OB-CONF-002", "OB-REF-001"],
            reported.Select(finding => finding.GetProperty("checkId").GetString()));
        Assert.All(
            reported,
            finding => Assert.Equal("Error", finding.GetProperty("severity").GetString()));

        JsonElement provenance =
            await ApiFixtures.GetJsonAsync(client, $"/api/v1/domain/{stagingId}/provenance");

        Assert.Equal(stagingId, provenance.GetProperty("domainEntityId").GetGuid());

        JsonElement[] records = [.. provenance.GetProperty("records").EnumerateArray()];

        Assert.Equal(4, records.Length);
        Assert.Single(records, IsWholeEntity);
        Assert.Equal(3, records.Count(lineage => !IsWholeEntity(lineage)));
        Assert.All(
            records,
            lineage =>
            {
                Assert.Equal("CancerStaging", lineage.GetProperty("domainEntityType").GetString());
                Assert.Equal(
                    "FhirCancerStagingNormalization",
                    lineage.GetProperty("transformationName").GetString());
                Assert.Equal("1.0.0", lineage.GetProperty("transformationVersion").GetString());
            });

        Assert.Equal(
            new HashSet<Guid>
            {
                SourceIdOf(sourceResources, "staging-group-001"),
                SourceIdOf(sourceResources, "staging-t-001"),
                SourceIdOf(sourceResources, "staging-n-001"),
                SourceIdOf(sourceResources, "staging-m-001"),
            },
            records.Select(lineage => lineage.GetProperty("sourceResourceId").GetGuid()).ToHashSet());

        Assert.Equal(
            SourceIdOf(sourceResources, "staging-group-001"),
            Assert.Single(records, IsWholeEntity).GetProperty("sourceResourceId").GetGuid());
    }

    [Fact]
    public async Task The_acceptance_bundle_reports_none_of_the_checks_it_does_not_trip()
    {
        JsonElement findings = await (await AcceptanceImport.RunAsync(fixture.Client))
            .FindingsAsync(fixture.Client);

        string[] reported =
        [
            .. findings.EnumerateArray().Select(finding => finding.GetProperty("checkId").GetString()!),
        ];

        Assert.DoesNotContain("OB-STR-001", reported);
        Assert.DoesNotContain("OB-REF-002", reported);
        Assert.DoesNotContain("OB-DOM-001", reported);
    }

    private static async Task<Guid> PostAcceptanceBundleAsync(HttpClient client)
    {
        using HttpResponseMessage response =
            await ApiFixtures.PostBundleAsync(client, ApiFixtures.AcceptanceBundleBytes);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Guid importBatchId =
            (await ApiFixtures.ReadJsonAsync(response)).GetProperty("importBatchId").GetGuid();

        Assert.Equal(
            $"{ApiFixtures.ImportsRoute}/{importBatchId}",
            response.Headers.Location?.ToString());

        return importBatchId;
    }

    private static bool IsWholeEntity(JsonElement lineage) =>
        lineage.GetProperty("fieldPath").ValueKind == JsonValueKind.Null;

    private static Guid SourceIdOf(IEnumerable<JsonElement> sourceResources, string sourceLogicalId) =>
        sourceResources
            .Single(source => source.GetProperty("sourceLogicalId").GetString() == sourceLogicalId)
            .GetProperty("id")
            .GetGuid();
}
