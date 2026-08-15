using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed record StagingMethod(CodedConcept Code)
{
    public CodedConcept Code { get; } = Code ?? throw new ArgumentNullException(nameof(Code));

    public override string ToString() => Code.ToString();
}
