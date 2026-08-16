namespace OncoBridge.Domain.Quality;

public static class V1CheckIds
{
    public static CheckId UnparseableEntry { get; } = CheckId.Parse("OB-STR-001");

    public static CheckId UnresolvedReference { get; } = CheckId.Parse("OB-REF-001");

    public static CheckId StageGroupSubjectDisagreement { get; } = CheckId.Parse("OB-REF-002");

    public static CheckId PrimaryCancerConditionCategory { get; } = CheckId.Parse("OB-CONF-001");

    public static CheckId StageGroupMethod { get; } = CheckId.Parse("OB-CONF-002");

    public static CheckId StagingPrecedesDiagnosis { get; } = CheckId.Parse("OB-DOM-001");
}
