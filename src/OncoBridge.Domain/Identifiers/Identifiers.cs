namespace OncoBridge.Domain.Identifiers;

public readonly record struct PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct PrimaryCancerDiagnosisId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct ImportBatchId(Guid Value)
{
    public static ImportBatchId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public readonly record struct SourceResourceId(Guid Value)
{
    public static SourceResourceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
