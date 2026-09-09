using Gloria.Domain.Reference;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class SourceSystemRepository : ISourceSystemRepository
{
    private readonly GloriaDbContext _db;
    public SourceSystemRepository(GloriaDbContext db) => _db = db;

    public Task<SourceSystem?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.SourceSystems.FirstOrDefaultAsync(s => s.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<SourceSystem>> GetAllAsync(CancellationToken ct = default) =>
        await _db.SourceSystems.OrderBy(s => s.Code).ToListAsync(ct);

    public async Task AddAsync(SourceSystem source, CancellationToken ct = default) =>
        await _db.SourceSystems.AddAsync(source, ct);
}

public sealed class RuleTypeRepository : IRuleTypeRepository
{
    private readonly GloriaDbContext _db;
    public RuleTypeRepository(GloriaDbContext db) => _db = db;

    public Task<RuleType?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.RuleTypes.FirstOrDefaultAsync(t => t.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<RuleType>> GetAllAsync(CancellationToken ct = default) =>
        await _db.RuleTypes.OrderBy(t => t.Code).ToListAsync(ct);

    public async Task AddAsync(RuleType ruleType, CancellationToken ct = default) =>
        await _db.RuleTypes.AddAsync(ruleType, ct);
}
