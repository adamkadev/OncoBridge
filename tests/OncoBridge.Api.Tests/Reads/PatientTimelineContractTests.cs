using System.Text.Json;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PatientTimelineContractTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task The_acceptance_bundle_projects_three_established_groups()
    {
        JsonElement timeline = await ReadTimelineAsync();

        Assert.Equal([1, 2, 3], Groups(timeline).Select(group => group.GetProperty("sequence").GetInt32()));
        Assert.All(
            Groups(timeline),
            group => Assert.Equal("Established", group.GetProperty("kind").GetString()));
        Assert.All(Groups(timeline), group => Assert.Single(Events(group)));
    }

    [Fact]
    public async Task The_acceptance_bundle_sequences_the_diagnosis_the_staging_then_the_procedure()
    {
        JsonElement timeline = await ReadTimelineAsync();

        Assert.Equal(
            ["PrimaryCancerDiagnosis", "CancerStaging", "CancerSurgicalProcedure"],
            SequencedEvents(timeline).Select(sequenced => sequenced.GetProperty("entityKind").GetString()));
    }

    [Fact]
    public async Task Every_anchor_keeps_the_lexical_value_and_precision_the_source_stated()
    {
        JsonElement timeline = await ReadTimelineAsync();

        Assert.Equal(
            [("2019-03", "Month"), ("2019-04-02", "Day"), ("2019-05", "Month")],
            SequencedEvents(timeline).Select(sequenced => DateOf(sequenced.GetProperty("anchor"))));
    }

    [Fact]
    public async Task The_procedure_is_anchored_on_its_start_and_keeps_both_stated_bounds()
    {
        JsonElement timeline = await ReadTimelineAsync();

        JsonElement procedure = SequencedEvents(timeline)
            .Single(sequenced => sequenced.GetProperty("entityKind").GetString() == "CancerSurgicalProcedure");

        JsonElement occurrence = procedure.GetProperty("occurrence");

        Assert.Equal("Period", occurrence.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, occurrence.GetProperty("date").ValueKind);

        JsonElement period = occurrence.GetProperty("period");

        Assert.Equal(("2019-05", "Month"), DateOf(period.GetProperty("start")));
        Assert.Equal(("2019-06-12", "Day"), DateOf(period.GetProperty("end")));
        Assert.Equal(("2019-05", "Month"), DateOf(procedure.GetProperty("anchor")));
    }

    [Fact]
    public async Task The_acceptance_bundle_leaves_nothing_unsequenced()
    {
        JsonElement timeline = await ReadTimelineAsync();

        Assert.Empty(timeline.GetProperty("unsequencedEvents").EnumerateArray());
    }

    [Fact]
    public async Task The_response_states_the_projection_policy_the_reader_is_shown()
    {
        JsonElement policy = (await ReadTimelineAsync()).GetProperty("projectionPolicy");

        Assert.Equal("1.0.0", policy.GetProperty("version").GetString());
        Assert.Equal(
            "Events are sequenced by their temporal anchor, projected on stated bounds only. "
                + "A period is anchored by its stated start bound.",
            policy.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Every_event_carries_the_canonical_entity_id_the_inspector_navigates_to()
    {
        AcceptanceImport import = await AcceptanceImport.RunAsync(fixture.Client);
        JsonElement record = await import.RecordAsync(fixture.Client);
        JsonElement timeline = await import.TimelineAsync(fixture.Client);

        Assert.Equal(
            [
                Single(record, "primaryCancerDiagnoses").GetProperty("id").GetGuid(),
                Single(record, "cancerStagings").GetProperty("id").GetGuid(),
                Single(record, "cancerSurgicalProcedures").GetProperty("id").GetGuid(),
            ],
            SequencedEvents(timeline).Select(sequenced => sequenced.GetProperty("entityId").GetGuid()));
    }

    [Fact]
    public async Task The_diagnosis_event_carries_its_recorded_date_as_metadata()
    {
        JsonElement diagnosis = SequencedEvents(await ReadTimelineAsync())
            .Single(sequenced => sequenced.GetProperty("entityKind").GetString() == "PrimaryCancerDiagnosis");

        Assert.Equal(
            ("2019-04-02", "Day"),
            DateOf(diagnosis.GetProperty("diagnosis").GetProperty("recordedDate")));

        Assert.Equal(JsonValueKind.Null, diagnosis.GetProperty("staging").ValueKind);
        Assert.Equal(JsonValueKind.Null, diagnosis.GetProperty("procedure").ValueKind);
    }

    [Fact]
    public async Task The_staging_event_carries_its_stage_group_and_axis_ordered_categories()
    {
        JsonElement staging = SequencedEvents(await ReadTimelineAsync())
            .Single(sequenced => sequenced.GetProperty("entityKind").GetString() == "CancerStaging")
            .GetProperty("staging");

        Assert.Equal("IIA", staging.GetProperty("stageGroup").GetProperty("code").GetString());
        Assert.Equal(
            ["T", "N", "M"],
            staging.GetProperty("categories")
                .EnumerateArray()
                .Select(category => category.GetProperty("axis").GetString()));
    }

    [Fact]
    public void A_timeline_event_contract_exposes_no_sequence_of_its_own()
    {
        string[] offenders =
        [
            .. typeof(TimelineEventResponse).GetProperties()
                .Select(property => property.Name)
                .Where(name => name.Contains("Sequence", StringComparison.Ordinal)),
        ];

        Assert.True(
            offenders.Length == 0,
            "A sequence belongs to a group, never to an event inside one, but TimelineEventResponse "
                + $"exposes: {string.Join(", ", offenders)}.");
    }

    private static IEnumerable<JsonElement> Groups(JsonElement timeline) =>
        timeline.GetProperty("groups").EnumerateArray();

    private static IEnumerable<JsonElement> Events(JsonElement group) =>
        group.GetProperty("events").EnumerateArray();

    private static IEnumerable<JsonElement> SequencedEvents(JsonElement timeline) =>
        Groups(timeline).SelectMany(Events);

    private static (string? Value, string? Precision) DateOf(JsonElement date) =>
        (date.GetProperty("value").GetString(), date.GetProperty("precision").GetString());

    private static JsonElement Single(JsonElement record, string property) =>
        record.GetProperty(property).EnumerateArray().Single();

    private async Task<JsonElement> ReadTimelineAsync() =>
        await (await AcceptanceImport.RunAsync(fixture.Client)).TimelineAsync(fixture.Client);
}
