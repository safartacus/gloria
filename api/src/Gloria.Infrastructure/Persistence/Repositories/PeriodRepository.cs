using Gloria.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class PeriodRepository : IPeriodRepository
{
    private readonly GloriaDbContext _db;
    public PeriodRepository(GloriaDbContext db) => _db = db;

    public Task<Period?> GetAsync(int year, int month, CancellationToken ct = default) =>
        _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

    public async Task<Period> GetOrOpenAsync(int year, int month, DateTime now, CancellationToken ct = default)
    {
        var existing = await GetAsync(year, month, ct);
        if (existing is not null) return existing;

        var period = Period.Open(year, month, now);
        await _db.Periods.AddAsync(period, ct);
        return period;
    }

    public async Task<IReadOnlyList<Period>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Periods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync(ct);

    public async Task AddAsync(Period period, CancellationToken ct = default) =>
        await _db.Periods.AddAsync(period, ct);
}
