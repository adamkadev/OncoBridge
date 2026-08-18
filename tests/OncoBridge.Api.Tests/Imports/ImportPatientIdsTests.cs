using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Imports;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ImportPatientIdsTests(ApiPostgreSqlFixture fixture)
{
    private const string PatientOnlyBundle = """
        {
          "resourceType": "Bundle",
          "type": "collection",
          "entry": [
            {
              "fullUrl": "urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa",
              "resource": { "resourceType": "Patient", "id": "patient-001", "birthDate": "1968" }
            }
          ]
        }
        """;

    [Fact]
    public async Task The_acceptance_fixture_reports_exactly_one_canonical_patient()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);

        Guid patientId = Assert.Single(PatientIdsOf(imported.Import));

        JsonElement record = await imported.RecordAsync(fixture.Client);

        Assert.Equal(record.GetProperty("patient").GetProperty("id").GetGuid(), patientId);
    }

    [Fact]
    public async Task A_stored_patient_resource_that_normalized_to_nothing_reports_no_patient()
    {
        Guid importBatchId =
            await ApiFixtures.ImportAsync(fixture.Client, ApiFixtures.Utf8(PatientOnlyBundle));

        JsonElement import = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{importBatchId}");

        Assert.Equal(
            "Patient",
            Assert.Single(import.GetProperty("sourceResources").EnumerateArray())
                .GetProperty("resourceType")
                .GetString());

        Assert.Empty(PatientIdsOf(import));
    }

    [Fact]
    public async Task Patient_ids_name_rows_the_record_endpoint_can_serve()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);

        foreach (Guid patientId in PatientIdsOf(imported.Import))
        {
            using HttpResponseMessage response =
                await fixture.Client.GetAsync($"/api/v1/patients/{patientId}/record");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Patient_ids_are_returned_in_a_deterministic_order()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);

        JsonElement reread = await ApiFixtures.GetJsonAsync(
            fixture.Client, $"{ApiFixtures.ImportsRoute}/{imported.ImportBatchId}");

        Assert.Equal(PatientIdsOf(imported.Import), PatientIdsOf(reread));
        Assert.Equal(PatientIdsOf(reread).Order(), PatientIdsOf(reread));
    }

    private static Guid[] PatientIdsOf(JsonElement import) =>
        [.. import.GetProperty("patientIds").EnumerateArray().Select(id => id.GetGuid())];
}
