namespace Gloria.Domain.Reference;

public interface ISourceSystemRepository
{
    Task<SourceSystem?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<SourceSystem>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(SourceSystem source, CancellationToken ct = default);
}

public interface IRuleTypeRepository
{
    Task<RuleType?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<RuleType>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RuleType ruleType, CancellationToken ct = default);
}
