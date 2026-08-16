using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Oncology;

public sealed record StagingMethod
{
    private StagingMethod() => Code = null!;

    public StagingMethod(CodedConcept code)
    {
        ArgumentNullException.ThrowIfNull(code);

        Code = code;
    }

    public CodedConcept Code { get; }

    public override string ToString() => Code.ToString();
}
