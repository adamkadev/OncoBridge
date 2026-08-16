using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Infrastructure.Persistence.Configurations;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class QualityStore(OncoBridgeDbContext context) : IQualityStore
{
    public async Task<QualityAssessmentSource?> LoadAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default)
    {
        bool exists = await context.ImportBatches
            .AsNoTracking()
            .AnyAsync(batch => batch.Id == batchId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        List<SourceResource> sourceResources = await context.SourceResources
            .AsNoTracking()
            .Where(resource => resource.BatchId == batchId)
            .OrderBy(resource => resource.EntryIndex)
            .ToListAsync(cancellationToken);

        List<PrimaryCancerDiagnosis> diagnoses =
            await OwnedByBatch(context.PrimaryCancerDiagnoses, batchId).ToListAsync(cancellationToken);

        List<CancerStaging> stagings =
            await OwnedByBatch(context.CancerStagings, batchId).ToListAsync(cancellationToken);

        return new QualityAssessmentSource(batchId, sourceResources, diagnoses, stagings);
    }

    public async Task ReplaceFindingsAsync(
        ImportBatchId batchId,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(findings);

        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await OwnedByBatch(context.Findings, batchId).ExecuteDeleteAsync(cancellationToken);

            context.ChangeTracker.Clear();

            foreach (Finding finding in findings)
            {
                context.Add(finding).Property(CanonicalColumns.BatchIdProperty).CurrentValue = batchId;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            throw;
        }
    }

    private static IQueryable<TEntity> OwnedByBatch<TEntity>(
        DbSet<TEntity> set, ImportBatchId batchId)
        where TEntity : class =>
        set.AsNoTracking()
            .Where(entity =>
                EF.Property<ImportBatchId>(entity, CanonicalColumns.BatchIdProperty) == batchId);
}
