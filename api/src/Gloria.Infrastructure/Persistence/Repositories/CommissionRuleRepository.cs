using Gloria.Domain.CommissionRules;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly GloriaDbContext _db;
    public CommissionRuleRepository(GloriaDbContext db) => _db = db;

    private IQueryable<CommissionRule> WithGraph() =>
        _db.CommissionRules.Include(r => r.RuleType).Include(r => r.BracketTiers);

    public Task<CommissionRule?> GetByIdAsync(int id, CancellationToken ct = default) =>
        WithGraph().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<CommissionRule>> GetEffectiveRulesOnAsync(DateOnly date, CancellationToken ct = default) =>
        await WithGraph()
            .Where(r => r.EffectiveFrom <= date && (r.EffectiveTo == null ? r.IsActive : r.EffectiveTo >= date))
            .OrderBy(r => r.Priority).ThenByDescending(r => r.Version)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CommissionRule>> GetRulesEffectiveInPeriodAsync(int year, int month, CancellationToken ct = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return await WithGraph()
            .Where(r => r.EffectiveFrom <= monthEnd && (r.EffectiveTo == null || r.EffectiveTo >= monthStart))
            .OrderBy(r => r.Priority).ThenByDescending(r => r.Version)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommissionRule>> GetGroupVersionsAsync(string ruleGroupCode, CancellationToken ct = default) =>
        await WithGraph()
            .Where(r => r.RuleGroupCode == ruleGroupCode.ToUpperInvariant())
            .OrderBy(r => r.Version)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken ct = default) =>
        await WithGraph()
            .OrderBy(r => r.RuleGroupCode).ThenBy(r => r.Version)
            .ToListAsync(ct);

    public async Task AddAsync(CommissionRule rule, CancellationToken ct = default) =>
        await _db.CommissionRules.AddAsync(rule, ct);
}
