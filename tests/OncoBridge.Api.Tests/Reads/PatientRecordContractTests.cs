using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PatientRecordContractTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task A_coded_concept_is_returned_as_system_code_and_display()
    {
        JsonElement record = await ReadRecordAsync();

        JsonElement code = record.GetProperty("primaryCancerDiagnoses")
            .EnumerateArray()
            .Single()
            .GetProperty("code");

        Assert.Equal("http://snomed.info/sct", code.GetProperty("system").GetString());
        Assert.Equal("254837009", code.GetProperty("code").GetString());
        Assert.Equal(
            "Malignant neoplasm of breast (disorder)", code.GetProperty("display").GetString());
    }

    [Fact]
    public async Task A_staging_assessment_names_the_patient_and_the_diagnosis_it_belongs_to()
    {
        JsonElement record = await ReadRecordAsync();

        JsonElement staging = record.GetProperty("cancerStagings").EnumerateArray().Single();

        Assert.Equal(
            record.GetProperty("patient").GetProperty("id").GetGuid(),
            staging.GetProperty("patientId").GetGuid());
        Assert.Equal(
            record.GetProperty("primaryCancerDiagnoses").EnumerateArray().Single()
                .GetProperty("id").GetGuid(),
            staging.GetProperty("primaryCancerDiagnosisId").GetGuid());
    }

    [Fact]
    public async Task Each_stage_category_names_the_source_resource_it_was_read_from()
    {
        AcceptanceImport imported = await AcceptanceImport.RunAsync(fixture.Client);
        JsonElement record = await imported.RecordAsync(fixture.Client);

        Dictionary<string, Guid> byAxis = record.GetProperty("cancerStagings")
            .EnumerateArray()
            .Single()
            .GetProperty("categories")
            .EnumerateArray()
            .ToDictionary(
                category => category.GetProperty("axis").GetString()!,
                category => category.GetProperty("sourceResourceId").GetGuid());

        Assert.Equal(imported.SourceId("staging-t-001"), byAxis["T"]);
        Assert.Equal(imported.SourceId("staging-n-001"), byAxis["N"]);
        Assert.Equal(imported.SourceId("staging-m-001"), byAxis["M"]);
    }

    [Fact]
    public async Task A_stage_group_and_its_absent_method_are_both_reported()
    {
        JsonElement staging =
            (await ReadRecordAsync()).GetProperty("cancerStagings").EnumerateArray().Single();

        Assert.Equal("IIA", staging.GetProperty("stageGroup").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, staging.GetProperty("method").ValueKind);
    }

    [Fact]
    public async Task The_patient_keeps_the_identifier_the_source_stated()
    {
        JsonElement patient = (await ReadRecordAsync()).GetProperty("patient");

        Assert.Equal("SYN-0001", patient.GetProperty("sourceIdentifier").GetString());
    }

    private async Task<JsonElement> ReadRecordAsync() =>
        await (await AcceptanceImport.RunAsync(fixture.Client)).RecordAsync(fixture.Client);
}
