using System.Text.Json;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class TemporalPrecisionTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task A_birth_date_stated_as_a_year_stays_a_year()
    {
        JsonElement record = await ReadRecordAsync();

        AssertDate(record.GetProperty("patient").GetProperty("birthDate"), "1968", "Year");
    }

    [Fact]
    public async Task A_diagnosis_onset_stated_as_a_month_stays_a_month()
    {
        JsonElement record = await ReadRecordAsync();

        JsonElement onset = Single(record, "primaryCancerDiagnoses").GetProperty("onset");

        Assert.Equal("Date", onset.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, onset.GetProperty("period").ValueKind);
        AssertDate(onset.GetProperty("date"), "2019-03", "Month");
    }

    [Fact]
    public async Task A_staging_effective_date_stated_as_a_day_stays_a_day()
    {
        JsonElement record = await ReadRecordAsync();

        AssertDate(Single(record, "cancerStagings").GetProperty("effective"), "2019-04-02", "Day");
    }

    [Fact]
    public async Task A_performed_period_keeps_both_bounds_at_their_own_precision()
    {
        JsonElement record = await ReadRecordAsync();

        JsonElement performed = Single(record, "cancerSurgicalProcedures").GetProperty("performed");

        Assert.Equal("Period", performed.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, performed.GetProperty("date").ValueKind);

        JsonElement period = performed.GetProperty("period");

        AssertDate(period.GetProperty("start"), "2019-05", "Month");
        AssertDate(period.GetProperty("end"), "2019-06-12", "Day");
    }

    [Fact]
    public async Task A_recorded_date_stated_as_a_day_stays_a_day()
    {
        JsonElement record = await ReadRecordAsync();

        AssertDate(Single(record, "primaryCancerDiagnoses").GetProperty("recordedDate"), "2019-04-02", "Day");
    }

    private static void AssertDate(JsonElement date, string value, string precision)
    {
        Assert.Equal(value, date.GetProperty("value").GetString());
        Assert.Equal(precision, date.GetProperty("precision").GetString());
    }

    private static JsonElement Single(JsonElement record, string property) =>
        record.GetProperty(property).EnumerateArray().Single();

    private async Task<JsonElement> ReadRecordAsync() =>
        await (await AcceptanceImport.RunAsync(fixture.Client)).RecordAsync(fixture.Client);
}
