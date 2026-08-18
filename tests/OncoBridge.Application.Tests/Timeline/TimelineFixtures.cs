using System.Globalization;
using OncoBridge.Application.Reading;
using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Application.Tests.Timeline;

internal static class TimelineFixtures
{
    internal static readonly PatientId Subject = new(Id(9000));

    internal static readonly PrimaryCancerDiagnosisId DiagnosisOf = new(Id(1));

    internal static readonly CodedConcept BreastCancer =
        new("http://snomed.info/sct", "254837009", "Malignant neoplasm of breast (disorder)");

    internal static readonly CodedConcept Lumpectomy =
        new("http://snomed.info/sct", "392021009", "Lumpectomy of breast (procedure)");

    internal static readonly CodedConcept StageIIA = new("http://cancerstaging.org", "IIA", "Stage IIA");

    internal static Guid Id(int seed) =>
        new($"{seed:D8}-0000-0000-0000-000000000000");

    internal static PartialDate Year(int year) => PartialDate.FromYear(year);

    internal static PartialDate Month(int year, int month) => PartialDate.FromYearMonth(year, month);

    internal static PartialDate Day(int year, int month, int day) => PartialDate.FromDate(year, month, day);

    internal static PartialDate Instant(string value) => PartialDate.FromInstant(
        DateTimeOffset.ParseExact(value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));

    internal static TemporalOccurrence At(PartialDate date) => TemporalOccurrence.FromDate(date);

    internal static TemporalOccurrence Between(PartialDate start, PartialDate end) =>
        TemporalOccurrence.FromPeriod(PartialPeriod.Between(start, end));

    internal static TemporalOccurrence StartingAt(PartialDate start) =>
        TemporalOccurrence.FromPeriod(PartialPeriod.StartingAt(start));

    internal static TemporalOccurrence EndingAt(PartialDate end) =>
        TemporalOccurrence.FromPeriod(PartialPeriod.EndingAt(end));

    internal static PrimaryCancerDiagnosis Diagnosis(
        TemporalOccurrence? onset = null,
        PartialDate? recordedDate = null,
        CodedConcept? code = null,
        int seed = 1) =>
        new(
            new PrimaryCancerDiagnosisId(Id(seed)),
            Subject,
            code ?? BreastCancer,
            onset,
            recordedDate: recordedDate);

    internal static CancerStaging Staging(
        PartialDate? effective = null,
        CodedConcept? stageGroup = null,
        IEnumerable<StageCategory>? categories = null,
        int seed = 2) =>
        new(
            Id(seed),
            Subject,
            DiagnosisOf,
            stageGroup ?? (categories is null ? StageIIA : null),
            effective: effective,
            categories: categories);

    internal static CancerSurgicalProcedure Procedure(
        TemporalOccurrence? performed = null,
        CodedConcept? code = null,
        int seed = 3) =>
        new(Id(seed), Subject, code ?? Lumpectomy, performed);

    internal static StageCategory Category(StageAxis axis, string code) =>
        new(axis, new CodedConcept("http://cancerstaging.org", code), new SourceResourceId(Id(500)));

    internal static PatientRecord Record(
        IEnumerable<PrimaryCancerDiagnosis>? diagnoses = null,
        IEnumerable<CancerStaging>? stagings = null,
        IEnumerable<CancerSurgicalProcedure>? procedures = null,
        PartialDate? birthDate = null) =>
        new()
        {
            Patient = new Patient(Subject, "SYN-0001", birthDate),
            PrimaryCancerDiagnoses = [.. diagnoses ?? []],
            CancerStagings = [.. stagings ?? []],
            CancerSurgicalProcedures = [.. procedures ?? []],
        };

    internal static TimelineEvent Only(PatientTimeline timeline) =>
        Assert.Single(Assert.Single(timeline.Groups).Events);
}
