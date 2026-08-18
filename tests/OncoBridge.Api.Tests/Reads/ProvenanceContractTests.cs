using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ProvenanceContractTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Field_level_lineage_names_the_field_path_it_derived()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);
        JsonElement record = await imported.RecordAsync(fixture.Client);

        Guid stagingId = record.GetProperty("cancerStagings")
            .EnumerateArray()
            .Single()
            .GetProperty("id")
            .GetGuid();

        JsonElement provenance =
            await ApiFixtures.GetJsonAsync(fixture.Client, $"/api/v1/domain/{stagingId}/provenance");

        string?[] fieldPaths =
        [
            .. provenance.GetProperty("records")
                .EnumerateArray()
                .Select(lineage => lineage.GetProperty("fieldPath").GetString()),
        ];

        Assert.Null(fieldPaths[0]);
        Assert.Equal(
            ["DistantMetastases", "PrimaryTumour", "RegionalNodes"],
            fieldPaths.Skip(1).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task A_patient_carries_whole_entity_lineage_only()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);

        JsonElement provenance = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"/api/v1/domain/{imported.PatientId}/provenance");

        JsonElement lineage = Assert.Single(provenance.GetProperty("records").EnumerateArray());

        Assert.Equal("Patient", lineage.GetProperty("domainEntityType").GetString());
        Assert.Equal(JsonValueKind.Null, lineage.GetProperty("fieldPath").ValueKind);
        Assert.Equal(imported.PatientId, lineage.GetProperty("sourceResourceId").GetGuid());
        Assert.Equal(
            "FhirPatientNormalization", lineage.GetProperty("transformationName").GetString());
    }
}
