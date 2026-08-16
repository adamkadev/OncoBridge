using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public enum StageAxis
{
    T,

    N,

    M,
}

public sealed record StageCategory
{
    private StageCategory() => Code = null!;

    public StageCategory(StageAxis axis, CodedConcept code, SourceResourceId sourceResourceId)
    {
        ArgumentNullException.ThrowIfNull(code);

        Axis = axis;
        Code = code;
        SourceResourceId = sourceResourceId;
    }

    public StageAxis Axis { get; }

    public CodedConcept Code { get; }

    public SourceResourceId SourceResourceId { get; }

    public string? Display => Code.Display;

    public override string ToString() => $"{Axis}: {Code}";
}
