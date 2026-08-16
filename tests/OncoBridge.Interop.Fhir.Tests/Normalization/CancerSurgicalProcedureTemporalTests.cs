using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerSurgicalProcedureTemporalTests
{
    private static CancerSurgicalProcedure FixtureProcedure() =>
        Assert.Single(NormalizationFixtures.NormalizeSurgicalProcedureBundle().CancerSurgicalProcedures);

    [Fact]
    public void A_month_precision_performed_date_time_stays_at_month_precision()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedDateTime":"2019-05" """);

        Assert.Equal(TemporalOccurrenceKind.Date, procedure.Performed!.Kind);
        Assert.Equal(PartialDate.FromYearMonth(2019, 5), procedure.Performed.Date);
        Assert.Equal(DatePrecision.Month, procedure.Performed.Date!.Precision);
        Assert.Null(procedure.Performed.Date.Day);
    }

    [Fact]
    public void A_year_precision_performed_date_time_is_not_widened_to_a_full_date()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedDateTime":"2019" """);

        Assert.Equal(PartialDate.FromYear(2019), procedure.Performed!.Date);
        Assert.Null(procedure.Performed.Date!.Month);
    }

    [Fact]
    public void A_full_performed_time_stamp_keeps_its_instant_and_stated_offset()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedDateTime":"2019-05-10T14:30:00+02:00" """);

        PartialDate performed = procedure.Performed!.Date!;

        Assert.Equal(DatePrecision.Instant, performed.Precision);
        Assert.Equal(
            new DateTimeOffset(2019, 5, 10, 14, 30, 0, TimeSpan.FromHours(2)), performed.Instant);
        Assert.Equal(TimeSpan.FromHours(2), performed.Instant!.Value.Offset);
    }

    [Fact]
    public void A_performed_period_keeps_both_boundaries_at_their_own_precision()
    {
        CancerSurgicalProcedure procedure = FixtureProcedure();

        Assert.Equal(TemporalOccurrenceKind.Period, procedure.Performed!.Kind);

        PartialPeriod performed = procedure.Performed.Period!;

        Assert.Equal(PartialDate.FromYearMonth(2019, 5), performed.Start);
        Assert.Equal(DatePrecision.Month, performed.Start!.Precision);
        Assert.Equal(PartialDate.FromDate(2019, 6, 12), performed.End);
        Assert.Equal(DatePrecision.Day, performed.End!.Precision);
    }

    [Fact]
    public void A_performed_period_with_only_a_start_is_not_completed()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedPeriod":{"start":"2019-05"} """);

        PartialPeriod performed = procedure.Performed!.Period!;

        Assert.Equal(PartialDate.FromYearMonth(2019, 5), performed.Start);
        Assert.True(performed.IsUnboundedEnd);
    }

    [Fact]
    public void A_performed_period_with_only_an_end_is_not_completed()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedPeriod":{"end":"2019-06-12"} """);

        PartialPeriod performed = procedure.Performed!.Period!;

        Assert.True(performed.IsUnboundedStart);
        Assert.Equal(PartialDate.FromDate(2019, 6, 12), performed.End);
    }

    [Fact]
    public void A_performed_string_is_not_parsed_into_a_temporal_value()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedString":"about three years ago" """);

        Assert.Null(procedure.Performed);
    }

    [Fact]
    public void An_unsupported_performed_representation_does_not_fabricate_a_temporal_value()
    {
        const string Ucum = "http://unitsofmeasure.org";
        const string PerformedAge =
            $$""" "performedAge":{"value":51,"unit":"years","system":"{{Ucum}}","code":"a"} """;

        Assert.Null(ProcedureFixtures.NormalizeProcedureWith(PerformedAge).Performed);
    }

    [Fact]
    public void A_contradictory_performed_period_leaves_no_occurrence_and_aborts_nothing()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """ "performedPeriod":{"start":"2019-06-12","end":"2019-05-10"} """);

        Assert.Null(procedure.Performed);
        Assert.Equal(ProcedureFixtures.LumpectomySnomedCode, procedure.Code.Code);
    }

    [Fact]
    public void An_absent_performed_element_leaves_no_occurrence()
    {
        Assert.Null(ProcedureFixtures.NormalizeProcedureWith().Performed);
    }
}
