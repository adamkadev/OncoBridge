using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Quality;

public sealed class DomainQualityEvaluator
{
    public DomainQualityAssessment Assess(
        IReadOnlyList<PrimaryCancerDiagnosis> diagnoses,
        IReadOnlyList<CancerStaging> stagings)
    {
        ArgumentNullException.ThrowIfNull(diagnoses);
        ArgumentNullException.ThrowIfNull(stagings);

        Dictionary<PrimaryCancerDiagnosisId, PrimaryCancerDiagnosis> stagedCancers =
            diagnoses.ToDictionary(diagnosis => diagnosis.Id);

        List<Finding> findings = [];
        List<CoverageNote> notes = [];

        foreach (CancerStaging staging in stagings.OrderBy(staging => staging.Id))
        {
            if (!stagedCancers.TryGetValue(
                staging.PrimaryCancerDiagnosisId, out PrimaryCancerDiagnosis? diagnosis))
            {
                throw new InvalidOperationException(
                    $"Staging '{staging.Id}' names primary cancer diagnosis "
                        + $"'{staging.PrimaryCancerDiagnosisId}', which is not among the supplied "
                        + "diagnoses; a staging may only be assessed against the cancer it stages.");
            }

            Evaluate(staging, diagnosis, findings, notes);
        }

        return new DomainQualityAssessment { Findings = findings, CoverageNotes = notes };
    }

    private static void Evaluate(
        CancerStaging staging,
        PrimaryCancerDiagnosis diagnosis,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        if (staging.Effective is not { } effective || diagnosis.Onset is not { } onset)
        {
            return;
        }

        if (EarliestStatedOnset(onset) is not { } onsetStart)
        {
            notes.Add(CoverageNote.Create(
                $"{nameof(PrimaryCancerDiagnosis)}.Onset period without a stated start",
                "The onset period states no start boundary, so whether staging preceded onset "
                    + "cannot be established.",
                TargetOf(staging)));

            return;
        }

        switch (PartialDate.Compare(effective, onsetStart))
        {
            case TemporalComparison.Before:
                findings.Add(StagingPrecedesDiagnosis(staging, effective, onsetStart));
                break;

            case TemporalComparison.Indeterminate:
                notes.Add(IndeterminateOrdering(staging, effective, onsetStart));
                break;
        }
    }

    private static PartialDate? EarliestStatedOnset(TemporalOccurrence onset) =>
        onset.Kind == TemporalOccurrenceKind.Date ? onset.Date : onset.Period!.Start;

    private static Finding StagingPrecedesDiagnosis(
        CancerStaging staging, PartialDate effective, PartialDate onsetStart) => Finding.Create(
        V1CheckIds.StagingPrecedesDiagnosis,
        FindingCategory.DomainConsistency,
        FindingSeverity.Warning,
        "The staging effective time is definitely before the onset of the primary cancer "
            + "diagnosis it stages.",
        TargetOf(staging),
        DomainQualityCitations.VariablePrecisionTemporalModel,
        expected: "staging effective time not definitely before diagnosis onset",
        actual: Describe(effective, onsetStart));

    private static CoverageNote IndeterminateOrdering(
        CancerStaging staging, PartialDate effective, PartialDate onsetStart) => CoverageNote.Create(
        $"{nameof(CancerStaging)}.Effective against {nameof(PrimaryCancerDiagnosis)}.Onset",
        "The precision at which these values were stated admits no definite ordering, so no "
            + $"claim was made. {Describe(effective, onsetStart)}.",
        TargetOf(staging));

    private static FindingTarget TargetOf(CancerStaging staging) =>
        FindingTarget.ForDomainEntity(nameof(CancerStaging), staging.Id);

    private static string Describe(PartialDate effective, PartialDate onsetStart) =>
        $"staging effective: {effective}; diagnosis onset: {onsetStart}";
}
