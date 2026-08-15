using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>
/// The staging system a <see cref="CancerStaging"/> assessment was performed under.
/// </summary>
/// <remarks>
/// A distinct type rather than a bare <see cref="CodedConcept"/> so that a staging method cannot be
/// passed where a stage group or a category value is expected — three codes that are structurally
/// identical and semantically unrelated.
/// </remarks>
/// <param name="Code">The staging method code as supplied.</param>
public sealed record StagingMethod(CodedConcept Code)
{
    /// <summary>The staging method code as supplied.</summary>
    public CodedConcept Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    /// <inheritdoc/>
    public override string ToString() => Code.ToString();
}
