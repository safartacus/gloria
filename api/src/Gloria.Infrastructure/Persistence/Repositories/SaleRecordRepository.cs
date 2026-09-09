using Gloria.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class SaleRecordRepository : ISaleRecordRepository
{
    private readonly GloriaDbContext _db;
    public SaleRecordRepository(GloriaDbContext db) => _db = db;

    public Task<bool> ExistsAsync(int sourceSystemId, string sourceTransactionNo, CancellationToken ct = default) =>
        _db.SaleRecords.AnyAsync(s => s.SourceSystemId == sourceSystemId && s.SourceTransactionNo == sourceTransactionNo, ct);

    public async Task<HashSet<string>> GetExistingKeysAsync(CancellationToken ct = default)
    {
        var pairs = await _db.SaleRecords
            .Select(s => new { s.SourceSystemId, s.SourceTransactionNo })
            .ToListAsync(ct);
        return pairs.Select(p => $"{p.SourceSystemId}|{p.SourceTransactionNo}").ToHashSet();
    }

    public async Task<IReadOnlyList<SaleRecord>> GetByPeriodAndEmployeeAsync(int year, int month, int employeeId, CancellationToken ct = default) =>
        await _db.SaleRecords
            .Where(s => s.Year == year && s.Month == month && s.EmployeeId == employeeId)
            .OrderBy(s => s.TransactionDate).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SaleRecord>> GetByPeriodAsync(int year, int month, CancellationToken ct = default) =>
        await _db.SaleRecords
            .Where(s => s.Year == year && s.Month == month)
            .OrderBy(s => s.EmployeeId).ThenBy(s => s.TransactionDate).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task AddAsync(SaleRecord sale, CancellationToken ct = default) =>
        await _db.SaleRecords.AddAsync(sale, ct);

    public async Task AddRangeAsync(IEnumerable<SaleRecord> sales, CancellationToken ct = default) =>
        await _db.SaleRecords.AddRangeAsync(sales, ct);
}
