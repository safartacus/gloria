namespace Gloria.Domain.Auditing;

public interface IAuditEntryRepository
{
    Task<AuditEntry?> GetLastAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetEntityHistoryAsync(string entityName, string entityId, CancellationToken ct = default);
    Task AddAsync(AuditEntry entry, CancellationToken ct = default);
}
