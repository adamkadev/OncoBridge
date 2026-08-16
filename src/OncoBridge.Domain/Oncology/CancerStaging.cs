using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed class CancerStaging
{
    private readonly List<StageCategory> _categories;

    public CancerStaging(
        Guid id,
        PatientId patientId,
        PrimaryCancerDiagnosisId primaryCancerDiagnosisId,
        CodedConcept? stageGroup = null,
        StagingMethod? method = null,
        PartialDate? effective = null,
        IEnumerable<StageCategory>? categories = null)
    {
        List<StageCategory> supplied = categories?.ToList() ?? [];

        if (supplied.Any(c => c is null))
        {
            throw new ArgumentException("Categories must not contain nulls.", nameof(categories));
        }

        IEnumerable<StageAxis> duplicatedAxes = supplied
            .GroupBy(c => c.Axis)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (duplicatedAxes.Any())
        {
            throw new ArgumentException(
                $"A staging assessment may hold at most one category per axis; duplicated: "
                    + $"{string.Join(", ", duplicatedAxes)}.",
                nameof(categories));
        }

        if (stageGroup is null && supplied.Count == 0)
        {
            throw new ArgumentException(
                "A staging assessment must state either a stage group or at least one category.",
                nameof(stageGroup));
        }

        Id = id;
        PatientId = patientId;
        PrimaryCancerDiagnosisId = primaryCancerDiagnosisId;
        StageGroup = stageGroup;
        Method = method;
        Effective = effective;
        _categories = supplied;
    }

    public Guid Id { get; }

    public PatientId PatientId { get; }

    public PrimaryCancerDiagnosisId PrimaryCancerDiagnosisId { get; }

    public CodedConcept? StageGroup { get; }

    public StagingMethod? Method { get; }

    public PartialDate? Effective { get; }

    public IReadOnlyList<StageCategory> Categories => _categories;

    public StageCategory? PrimaryTumour => FindAxis(StageAxis.T);

    public StageCategory? RegionalNodes => FindAxis(StageAxis.N);

    public StageCategory? DistantMetastases => FindAxis(StageAxis.M);

    public IReadOnlyCollection<SourceResourceId> CategorySourceResources =>
        _categories.Select(c => c.SourceResourceId).Distinct().ToList();

    private StageCategory? FindAxis(StageAxis axis) =>
        _categories.SingleOrDefault(c => c.Axis == axis);
}
