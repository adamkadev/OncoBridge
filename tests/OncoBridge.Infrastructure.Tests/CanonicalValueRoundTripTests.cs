using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;
using OncoBridge.Infrastructure.Persistence;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CanonicalValueRoundTripTests(PostgreSqlFixture postgres)
{
    private async Task<NormalizationScenario> NormalizedFixtureAsync()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);

        await scenario.NormalizeAsync(await scenario.IngestCompleteBundleAsync());

        return scenario;
    }

    private async Task<NormalizationScenario> NormalizedAsync(string bundle)
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);

        await scenario.NormalizeAsync(
            await scenario.IngestAsync(SyntheticFixtures.Utf8(bundle), "phase3d-temporal"));

        return scenario;
    }

    [Fact]
    public async Task A_coded_concept_round_trips_its_system_code_and_display_unchanged()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Equal(
            new CodedConcept(
                "http://snomed.info/sct", "254837009", "Malignant neoplasm of breast (disorder)"),
            (await scenario.SingleDiagnosisAsync()).Code);

        Assert.Equal(
            new CodedConcept("http://cancerstaging.org", "IIA", "Stage IIA"),
            (await scenario.SingleStagingAsync()).StageGroup);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "254292007", "Tumor staging (procedure)"),
            (await scenario.SingleStagingAsync()).Method!.Code);
    }

    [Fact]
    public async Task An_absent_coded_concept_round_trips_as_absent()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Null((await scenario.SinglePatientAsync()).SexAtBirthAsRecorded);
    }

    [Fact]
    public async Task A_year_precision_birth_date_round_trips_without_gaining_a_month_or_day()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        PartialDate birthDate = (await scenario.SinglePatientAsync()).BirthDate!;

        Assert.Equal(DatePrecision.Year, birthDate.Precision);
        Assert.Equal(PartialDate.FromYear(1968), birthDate);
        Assert.Null(birthDate.Month);
        Assert.Null(birthDate.Day);
    }

    [Fact]
    public async Task A_month_precision_onset_round_trips_as_a_date_occurrence_at_month_precision()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        TemporalOccurrence onset = (await scenario.SingleDiagnosisAsync()).Onset!;

        Assert.Equal(TemporalOccurrenceKind.Date, onset.Kind);
        Assert.Null(onset.Period);
        Assert.Equal(PartialDate.FromYearMonth(2019, 3), onset.Date);
        Assert.Equal(DatePrecision.Month, onset.Date!.Precision);
    }

    [Fact]
    public async Task A_day_precision_recorded_date_and_staging_effective_date_round_trip()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Equal(
            PartialDate.FromDate(2019, 4, 2), (await scenario.SingleDiagnosisAsync()).RecordedDate);
        Assert.Equal(
            PartialDate.FromDate(2019, 4, 2), (await scenario.SingleStagingAsync()).Effective);
    }

    [Fact]
    public async Task A_performed_period_round_trips_each_bound_at_its_own_precision()
    {
        NormalizationScenario scenario = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        TemporalOccurrence performed = (await scenario.SingleProcedureAsync()).Performed!;

        Assert.Equal(TemporalOccurrenceKind.Period, performed.Kind);
        Assert.Null(performed.Date);

        PartialPeriod period = performed.Period!;

        Assert.Equal(PartialDate.FromYearMonth(2019, 5), period.Start);
        Assert.Equal(DatePrecision.Month, period.Start!.Precision);
        Assert.Equal(PartialDate.FromDate(2019, 6, 12), period.End);
        Assert.Equal(DatePrecision.Day, period.End!.Precision);
    }

    [Fact]
    public async Task An_instant_onset_round_trips_with_its_stated_utc_offset_intact()
    {
        NormalizationScenario scenario = await NormalizedAsync(TemporalFixtures.InstantOnsetBundle);
        await using OncoBridgeDbContext _context = scenario.Context;

        PartialDate onset = (await scenario.SingleDiagnosisAsync()).Onset!.Date!;

        Assert.Equal(DatePrecision.Instant, onset.Precision);
        Assert.Equal(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2)), onset.Instant);
        Assert.Equal(TimeSpan.FromHours(2), onset.Instant!.Value.Offset);
    }

    [Fact]
    public async Task An_open_ended_performed_period_round_trips_without_gaining_a_bound()
    {
        NormalizationScenario scenario = await NormalizedAsync(TemporalFixtures.OpenEndedPeriodBundle);
        await using OncoBridgeDbContext _context = scenario.Context;

        PartialPeriod performed = (await scenario.SingleProcedureAsync()).Performed!.Period!;

        Assert.Equal(PartialDate.FromYear(2019), performed.Start);
        Assert.True(performed.IsUnboundedEnd);
        Assert.Null(performed.End);
    }

    [Fact]
    public async Task An_absent_occurrence_round_trips_as_absent()
    {
        NormalizationScenario scenario = await NormalizedAsync(TemporalFixtures.OpenEndedPeriodBundle);
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Null((await scenario.SingleDiagnosisAsync()).Onset);
        Assert.Null((await scenario.SingleDiagnosisAsync()).RecordedDate);
    }
}
