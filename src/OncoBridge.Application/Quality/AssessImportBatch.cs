using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Application.Quality;

public sealed class AssessImportBatch(
    ISourceQualityEvaluator sourceEvaluator,
    DomainQualityEvaluator domainEvaluator,
    IQualityStore store)
{
    public async Task<QualityAssessment?> ExecuteAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default)
    {
        if (await store.LoadAsync(batchId, cancellationToken) is not { } source)
        {
            return null;
        }

        SourceQualityAssessment sourceQuality = sourceEvaluator.Assess(source.SourceResources);

        DomainQualityAssessment domainQuality = domainEvaluator.Assess(
            source.PrimaryCancerDiagnoses, source.CancerStagings);

        Finding[] findings = [.. sourceQuality.Findings, .. domainQuality.Findings];

        await store.ReplaceFindingsAsync(batchId, findings, cancellationToken);

        return new QualityAssessment
        {
            Findings = findings,
            CoverageNotes = [.. sourceQuality.CoverageNotes, .. domainQuality.CoverageNotes],
        };
    }
}
