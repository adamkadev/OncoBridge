using Microsoft.EntityFrameworkCore;
using OncoBridge.Application.Imports;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class ImportBatchStore(OncoBridgeDbContext context) : IImportBatchWriter
{
    public async Task SaveAsync(
        ImportBatch batch,
        IReadOnlyList<SourceResource> sourceResources,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(sourceResources);

        context.ImportBatches.Add(batch);
        context.SourceResources.AddRange(sourceResources);

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<ImportBatch?> FindBatchAsync(
        ImportBatchId id, CancellationToken cancellationToken = default) =>
        context.ImportBatches
            .AsNoTracking()
            .SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SourceResource>> GetSourceResourcesAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default) =>
        await context.SourceResources
            .AsNoTracking()
            .Where(resource => resource.BatchId == batchId)
            .OrderBy(resource => resource.EntryIndex)
            .ToListAsync(cancellationToken);

    public Task<int> CountBatchesAsync(CancellationToken cancellationToken = default) =>
        context.ImportBatches.CountAsync(cancellationToken);
}
