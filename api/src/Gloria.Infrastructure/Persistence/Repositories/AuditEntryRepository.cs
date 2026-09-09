using Gloria.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class AuditEntryRepository : IAuditEntryRepository
{
    private readonly GloriaDbContext _db;
    public AuditEntryRepository(GloriaDbContext db) => _db = db;

    public Task<AuditEntry?> GetLastAsync(CancellationToken ct = default) =>
        _db.AuditEntries.OrderByDescending(a => a.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AuditEntry>> GetEntityHistoryAsync(string entityName, string entityId, CancellationToken ct = default) =>
        await _db.AuditEntries
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

    public async Task AddAsync(AuditEntry entry, CancellationToken ct = default) =>
        await _db.AuditEntries.AddAsync(entry, ct);
}
