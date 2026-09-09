using Gloria.Domain.Calculations;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class CommissionResultRepository : ICommissionResultRepository
{
    private readonly GloriaDbContext _db;
    public CommissionResultRepository(GloriaDbContext db) => _db = db;

    public Task<CommissionResult?> GetAsync(int periodId, int employeeId, CancellationToken ct = default) =>
        _db.CommissionResults
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.PeriodId == periodId && r.EmployeeId == employeeId, ct);

    public async Task<IReadOnlyList<CommissionResult>> GetByPeriodAsync(int periodId, CancellationToken ct = default) =>
        await _db.CommissionResults
            .Include(r => r.Steps)
            .Where(r => r.PeriodId == periodId)
            .OrderBy(r => r.EmployeeId)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<CommissionResult> Items, int TotalCount)> GetByPeriodPageAsync(
        int periodId, int skip, int take, string? sortBy, bool descending, CancellationToken ct = default)
    {
        var query = _db.CommissionResults.Where(r => r.PeriodId == periodId);
        var total = await query.CountAsync(ct);

        IOrderedQueryable<Domain.Calculations.CommissionResult> ordered = (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "employeename" => descending
                ? query.OrderByDescending(r => _db.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.FullName).FirstOrDefault())
                : query.OrderBy(r => _db.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.FullName).FirstOrDefault()),
            "salecount" => descending ? query.OrderByDescending(r => r.SaleCount) : query.OrderBy(r => r.SaleCount),
            "refundcount" => descending ? query.OrderByDescending(r => r.RefundCount) : query.OrderBy(r => r.RefundCount),
            "unmatchedcount" => descending ? query.OrderByDescending(r => r.UnmatchedCount) : query.OrderBy(r => r.UnmatchedCount),
            "status" => descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "calculatedat" => descending ? query.OrderByDescending(r => r.CalculatedAt) : query.OrderBy(r => r.CalculatedAt),
            _ => descending ? query.OrderByDescending(r => r.TotalCommission) : query.OrderBy(r => r.TotalCommission),
        };

        var items = await ordered
            .ThenBy(r => r.EmployeeId)          // stable tiebreaker across pages
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<PeriodCommissionTotals> GetPeriodTotalsAsync(int periodId, CancellationToken ct = default)
    {
        var query = _db.CommissionResults.Where(r => r.PeriodId == periodId);

        return new PeriodCommissionTotals(
            EmployeeCount: await query.CountAsync(ct),
            TotalCommission: await query.SumAsync(r => (decimal?)r.TotalCommission, ct) ?? 0m,
            RefundCount: await query.SumAsync(r => (int?)r.RefundCount, ct) ?? 0,
            UnmatchedCount: await query.SumAsync(r => (int?)r.UnmatchedCount, ct) ?? 0);
    }

    public async Task AddAsync(CommissionResult result, CancellationToken ct = default) =>
        await _db.CommissionResults.AddAsync(result, ct);
}
