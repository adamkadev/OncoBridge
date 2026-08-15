using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>
/// A staging assessment: an overall stage group and up to one category per axis, assembled into a
/// single concept from what the source scattered across several separate resources.
/// </summary>
/// <remarks>
/// <para>
/// This aggregate is the architectural centre of OncoBridge. In the interchange format a stage
/// group and its T, N and M categories are four sibling resources joined by references; here they
/// are one concept with its own invariants. That collapse — a graph in the source becoming an
/// aggregate in the domain — is what distinguishes normalisation from rendering.
/// </para>
/// <para><b>Invariants enforced (genuine structural ones only):</b></para>
/// <list type="number">
///   <item><description>At most one category per axis. Two T categories in one assessment is a contradiction, not a data-quality opinion.</description></item>
///   <item><description>
///     The assessment must assert something: either a stage group or at least one category.
///     An aggregate with neither carries no information.
///   </description></item>
/// </list>
/// <para><b>Deliberately NOT an invariant: a missing <see cref="Method"/>.</b> The interoperability
/// profile makes the staging method mandatory, so its absence is a <i>conformance finding against
/// the source</i> — not a reason to reject a domain object. If OncoBridge refused to build the
/// aggregate, it could never report the finding, and the very defect the system exists to surface
/// would become invisible. <see cref="Method"/> is therefore nullable by design. This distinction
/// between domain invariants and conformance findings is the point of ADR-0004.</para>
/// </remarks>
public sealed class CancerStaging
{
    private readonly List<StageCategory> _categories;

    /// <summary>Creates a staging assessment.</summary>
    /// <param name="id">This assessment's own identity.</param>
    /// <param name="patientId">The patient this assessment belongs to.</param>
    /// <param name="stageGroup">The overall stage group value as supplied, if stated.</param>
    /// <param name="method">
    /// The staging system used, if stated. Nullable by design — see the remarks on this type.
    /// </param>
    /// <param name="effective">When the assessment applies, if stated. Clinical time.</param>
    /// <param name="categories">
    /// The axis categories, at most one per axis. May be empty when a stage group is present.
    /// </param>
    /// <exception cref="ArgumentException">
    /// More than one category is supplied for the same axis, or neither a stage group nor any
    /// category is supplied.
    /// </exception>
    public CancerStaging(
        Guid id,
        PatientId patientId,
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
        StageGroup = stageGroup;
        Method = method;
        Effective = effective;
        _categories = supplied;
    }

    /// <summary>This assessment's own identity.</summary>
    public Guid Id { get; }

    /// <summary>The patient this assessment belongs to.</summary>
    public PatientId PatientId { get; }

    /// <summary>The overall stage group value as supplied, if stated.</summary>
    public CodedConcept? StageGroup { get; }

    /// <summary>
    /// The staging system used, if stated. Its absence is a conformance finding against the
    /// source, not a domain invariant violation.
    /// </summary>
    public StagingMethod? Method { get; }

    /// <summary>When the assessment applies, if stated. Clinical time, never authoring time.</summary>
    public PartialDate? Effective { get; }

    /// <summary>The axis categories held by this assessment, at most one per axis.</summary>
    public IReadOnlyList<StageCategory> Categories => _categories;

    /// <summary>The primary tumour category, if this assessment holds one.</summary>
    public StageCategory? PrimaryTumour => FindAxis(StageAxis.T);

    /// <summary>The regional nodes category, if this assessment holds one.</summary>
    public StageCategory? RegionalNodes => FindAxis(StageAxis.N);

    /// <summary>The distant metastases category, if this assessment holds one.</summary>
    public StageCategory? DistantMetastases => FindAxis(StageAxis.M);

    /// <summary>
    /// The distinct source resources this assessment was assembled from — the reason field-level
    /// lineage is worth recording for this concept and no other in V1.
    /// </summary>
    public IReadOnlyCollection<SourceResourceId> ContributingSourceResources =>
        _categories.Select(c => c.SourceResourceId).Distinct().ToList();

    private StageCategory? FindAxis(StageAxis axis) =>
        _categories.SingleOrDefault(c => c.Axis == axis);
}
