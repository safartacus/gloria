using Gloria.Application.Abstractions;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;

namespace Gloria.Infrastructure.Caching;

/// <summary>
/// Read-through cache decorators for reference data read only for scalar lookups (id / code). These
/// rows change only via a migration, so a 30-minute TTL is enough and no explicit invalidation is
/// needed. Single-item lookups are served from the cached "all" list.
/// Role / RuleType are deliberately NOT cached — they are attached as navigations to newly created
/// User / CommissionRule aggregates, where a detached cached instance would make EF re-insert them.
/// </summary>
public sealed class CachedSourceSystemRepository(ISourceSystemRepository inner, ICacheStore cache) : ISourceSystemRepository
{
    public async Task<SourceSystem?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        (await GetAllAsync(ct)).FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));

    public Task<IReadOnlyList<SourceSystem>> GetAllAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync(CacheKeys.SourceSystems, CacheKeys.ReferenceTtl, () => inner.GetAllAsync(ct), ct);

    public async Task AddAsync(SourceSystem source, CancellationToken ct = default)
    {
        await inner.AddAsync(source, ct);
        await cache.RemoveAsync(CacheKeys.SourceSystems, ct);
    }
}

public sealed class CachedDepartmentRepository(IDepartmentRepository inner, ICacheStore cache) : IDepartmentRepository
{
    public async Task<Department?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        (await GetAllAsync(ct)).FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));

    public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync(CacheKeys.Departments, CacheKeys.ReferenceTtl, () => inner.GetAllAsync(ct), ct);

    public async Task AddAsync(Department department, CancellationToken ct = default)
    {
        await inner.AddAsync(department, ct);
        await cache.RemoveAsync(CacheKeys.Departments, ct);
    }
}

public sealed class CachedEmployeeRepository(IEmployeeRepository inner, ICacheStore cache) : IEmployeeRepository
{
    public async Task<Employee?> GetByEmployeeNoAsync(string employeeNo, CancellationToken ct = default) =>
        (await GetAllAsync(ct)).FirstOrDefault(e => string.Equals(e.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase));

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
        (await GetAllAsync(ct)).FirstOrDefault(e => e.Id == id);

    public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync(CacheKeys.Employees, CacheKeys.ReferenceTtl, () => inner.GetAllAsync(ct), ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await inner.AddAsync(employee, ct);
        await cache.RemoveAsync(CacheKeys.Employees, ct);
    }
}
