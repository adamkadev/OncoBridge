using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Infrastructure.Persistence.Configurations;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class NormalizationStore(OncoBridgeDbContext context) : INormalizationStore
{
    public async Task<NormalizationSource?> LoadAsync(
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

        return new NormalizationSource(batchId, sourceResources);
    }

    public async Task ReplaceDerivedAsync(
        ImportBatchId batchId,
        NormalizationResult result,
        string normalizerVersion,
        DateTimeOffset normalizedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            ImportBatch batch = await context.ImportBatches
                .SingleAsync(candidate => candidate.Id == batchId, cancellationToken);

            await DeleteDerivedAsync(batchId, cancellationToken);

            context.ChangeTracker.Clear();
            context.Attach(batch);

            Insert(batchId, result);

            batch.MarkNormalized(normalizerVersion, normalizedAt);

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

    private async Task DeleteDerivedAsync(ImportBatchId batchId, CancellationToken cancellationToken)
    {
        await context.Findings
            .Where(finding =>
                EF.Property<ImportBatchId>(finding, CanonicalColumns.BatchIdProperty) == batchId
                && finding.Category == FindingCategory.DomainConsistency)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Lineages
            .Where(lineage => context.SourceResources
                .Any(resource => resource.Id == lineage.SourceResourceId && resource.BatchId == batchId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<StageCategory>()
            .Where(category => context.CancerStagings.Any(staging =>
                staging.Id
                    == EF.Property<Guid>(category, StageCategoryConfiguration.StagingIdProperty)
                && EF.Property<ImportBatchId>(staging, CanonicalColumns.BatchIdProperty) == batchId))
            .ExecuteDeleteAsync(cancellationToken);

        await DeleteOwnedByBatchAsync(context.CancerStagings, batchId, cancellationToken);
        await DeleteOwnedByBatchAsync(context.CancerSurgicalProcedures, batchId, cancellationToken);
        await DeleteOwnedByBatchAsync(context.PrimaryCancerDiagnoses, batchId, cancellationToken);
        await DeleteOwnedByBatchAsync(context.Patients, batchId, cancellationToken);
    }

    private static Task DeleteOwnedByBatchAsync<TEntity>(
        DbSet<TEntity> set, ImportBatchId batchId, CancellationToken cancellationToken)
        where TEntity : class =>
        set.Where(entity =>
                EF.Property<ImportBatchId>(entity, CanonicalColumns.BatchIdProperty) == batchId)
            .ExecuteDeleteAsync(cancellationToken);

    private void Insert(ImportBatchId batchId, NormalizationResult result)
    {
        AddOwnedByBatch(result.Patients, batchId);
        AddOwnedByBatch(result.PrimaryCancerDiagnoses, batchId);
        AddOwnedByBatch(result.CancerStagings, batchId);
        AddOwnedByBatch(result.CancerSurgicalProcedures, batchId);

        context.Lineages.AddRange(result.Lineage);
    }

    private void AddOwnedByBatch<TEntity>(IEnumerable<TEntity> entities, ImportBatchId batchId)
        where TEntity : class
    {
        foreach (TEntity entity in entities)
        {
            context.Add(entity).Property(CanonicalColumns.BatchIdProperty).CurrentValue = batchId;
        }
    }
}
