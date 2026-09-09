using Gloria.Domain.Imports;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class ImportBatchRepository : IImportBatchRepository
{
    private readonly GloriaDbContext _db;
    public ImportBatchRepository(GloriaDbContext db) => _db = db;

    public Task<ImportBatch?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.ImportBatches.Include(x => x.Errors).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ImportBatch>> GetRecentAsync(int count, CancellationToken ct = default) =>
        await _db.ImportBatches
            .OrderByDescending(x => x.StartedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportBatch>> GetByRunDateAsync(DateOnly runDate, CancellationToken ct = default)
    {
        // The requested date is the server's local calendar day (same clock the nightly job schedules
        // on); StartedAt is persisted in UTC, so translate the local-day window into UTC.
        var localMidnight = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var startUtc = localMidnight.ToUniversalTime();
        var endUtc = localMidnight.AddDays(1).ToUniversalTime();

        return await _db.ImportBatches
            .Include(x => x.Errors)
            .Where(x => x.StartedAt >= startUtc && x.StartedAt < endUtc)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ImportBatch batch, CancellationToken ct = default) =>
        await _db.ImportBatches.AddAsync(batch, ct);
}
