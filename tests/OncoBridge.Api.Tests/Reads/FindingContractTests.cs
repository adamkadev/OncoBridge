using System.Net;
using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class FindingContractTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task A_finding_carries_its_whole_evidence_shape()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);
        JsonElement findings = await imported.FindingsAsync(fixture.Client);

        JsonElement conformance = findings.EnumerateArray()
            .Single(finding => finding.GetProperty("checkId").GetString() == "OB-CONF-002");

        Assert.Equal("Conformance", conformance.GetProperty("category").GetString());
        Assert.Equal("Error", conformance.GetProperty("severity").GetString());
        Assert.Equal(
            "The TNM stage group does not state a staging method.",
            conformance.GetProperty("message").GetString());
        Assert.Equal(
            "https://hl7.org/fhir/us/mcode/STU4/StructureDefinition-mcode-tnm-stage-group.html",
            conformance.GetProperty("citation").GetString());
        Assert.Equal(
            "Observation.method to be present, which mCODE STU4 states as cardinality 1..1",
            conformance.GetProperty("expected").GetString());
        Assert.Equal("Observation.method is absent", conformance.GetProperty("actual").GetString());

        JsonElement target = conformance.GetProperty("target");

        Assert.Equal("SourceResource", target.GetProperty("kind").GetString());
        Assert.Equal(imported.SourceId("staging-group-001"), target.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, target.GetProperty("domainEntityType").ValueKind);
    }

    [Fact]
    public async Task Findings_are_ordered_by_check_id_then_target()
    {
        JsonElement findings = await (await AcceptanceImport.RunAsync(fixture.Client))
            .FindingsAsync(fixture.Client);

        (string CheckId, Guid Target)[] ordered =
        [
            .. findings.EnumerateArray().Select(finding => (
                finding.GetProperty("checkId").GetString()!,
                finding.GetProperty("target").GetProperty("id").GetGuid())),
        ];

        Assert.Equal(
            ordered.OrderBy(finding => finding.CheckId, StringComparer.Ordinal)
                .ThenBy(finding => finding.Target),
            ordered);
    }

    [Fact]
    public async Task An_import_with_nothing_to_report_returns_an_empty_array_not_a_204()
    {
        Guid importBatchId = await ApiFixtures.ImportAsync(
            fixture.Client,
            ApiFixtures.Utf8("""{"resourceType":"Bundle","type":"collection","entry":[]}"""));

        using HttpResponseMessage response = await fixture.Client.GetAsync(
            $"{ApiFixtures.ImportsRoute}/{importBatchId}/findings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await ApiFixtures.ReadJsonAsync(response)).EnumerateArray());
    }
}
