using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

/// <summary>The axis a <see cref="StageCategory"/> reports.</summary>
public enum StageAxis
{
    /// <summary>Primary tumour category.</summary>
    T,

    /// <summary>Regional nodes category.</summary>
    N,

    /// <summary>Distant metastases category.</summary>
    M,
}

/// <summary>
/// One axis of a staging assessment, together with the source resource it was drawn from.
/// </summary>
/// <remarks>
/// <para>
/// Each category arrives as a separate resource in the source and is reassembled into the
/// <see cref="CancerStaging"/> aggregate. Because a single aggregate is therefore built from several
/// distinct sources, each category carries its own <see cref="SourceResourceId"/> — this is the one
/// place in V1 where field-level lineage genuinely carries information.
/// </para>
/// <para>
/// <see cref="SourceResourceId"/> is an OncoBridge identity, never an interchange-format reference.
/// Provenance identity crosses into the domain as a plain identifier and nothing more (ADR-0001).
/// </para>
/// </remarks>
/// <param name="Axis">Which axis this category reports.</param>
/// <param name="Code">The category value as supplied.</param>
/// <param name="SourceResourceId">The source resource this category was drawn from.</param>
public sealed record StageCategory(
    StageAxis Axis,
    CodedConcept Code,
    SourceResourceId SourceResourceId)
{
    /// <summary>The category value as supplied.</summary>
    public CodedConcept Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    /// <summary>The display supplied by the source for this category, if any.</summary>
    public string? Display => Code.Display;

    /// <inheritdoc/>
    public override string ToString() => $"{Axis}: {Code}";
}
