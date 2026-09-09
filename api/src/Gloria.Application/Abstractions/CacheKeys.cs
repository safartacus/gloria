namespace Gloria.Application.Abstractions;

/// <summary>
/// Cache key / generation-key builders, shared by the infrastructure cache decorators and the
/// application query services. The physical "gloria:v1:" prefix is added by the cache store.
/// </summary>
public static class CacheKeys
{
    public static readonly TimeSpan ReferenceTtl = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan RuleTtl = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan CommissionTtl = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PeriodTtl = TimeSpan.FromMinutes(30);

    // Tier 1 — reference data read for scalar lookups: changes only via migration → TTL only.
    public const string SourceSystems = "ref:source-systems";
    public const string Departments = "ref:departments";
    public const string Employees = "ref:employees";

    // Commission rules — admin writes bump this generation.
    public const string RulesGen = "gen:rules";
    public static string RulesList(long gen) => $"rules:list:g{gen}";

    // Tier 2 — commission results: calculate / period-close bump the per-period generation.
    public static string CommissionGen(int year, int month) => $"gen:commissions:{year}:{month}";
    public static string CommissionPage(int year, int month, long gen, int page, int pageSize, string sortBy, string sortDir) =>
        $"commissions:{year}:{month}:g{gen}:list:p{page}:z{pageSize}:s{sortBy}:{sortDir}";
    public static string CommissionSummary(int year, int month, long gen) =>
        $"commissions:{year}:{month}:g{gen}:summary";
    public static string CommissionEmployee(int year, int month, long gen, int employeeId) =>
        $"commissions:{year}:{month}:g{gen}:emp:{employeeId}";

    // Periods list — open / close bump this generation.
    public const string PeriodsGen = "gen:periods";
    public static string PeriodsList(long gen) => $"periods:list:g{gen}";
}
