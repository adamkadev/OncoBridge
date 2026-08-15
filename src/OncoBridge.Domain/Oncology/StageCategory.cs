using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public enum StageAxis
{
    T,

    N,

    M,
}

public sealed record StageCategory(
    StageAxis Axis,
    CodedConcept Code,
    SourceResourceId SourceResourceId)
{
    public CodedConcept Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    public string? Display => Code.Display;

    public override string ToString() => $"{Axis}: {Code}";
}
