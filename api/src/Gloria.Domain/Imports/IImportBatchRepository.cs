namespace Gloria.Domain.Imports;

public interface IImportBatchRepository
{
    Task<ImportBatch?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ImportBatch>> GetRecentAsync(int count, CancellationToken ct = default);

    /// <summary>Batches whose run (<see cref="ImportBatch.StartedAt"/>) falls on the given UTC date, with their quarantined rows.</summary>
    Task<IReadOnlyList<ImportBatch>> GetByRunDateAsync(DateOnly runDate, CancellationToken ct = default);

    Task AddAsync(ImportBatch batch, CancellationToken ct = default);
}
