namespace Gloria.Domain.CommissionRules;

public interface ICommissionRuleRepository
{
    Task<CommissionRule?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>All rule versions effective (active + within date range) on a given date.</summary>
    Task<IReadOnlyList<CommissionRule>> GetEffectiveRulesOnAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>All rules that could be effective on any day of the given period (month).</summary>
    Task<IReadOnlyList<CommissionRule>> GetRulesEffectiveInPeriodAsync(int year, int month, CancellationToken ct = default);

    Task<IReadOnlyList<CommissionRule>> GetGroupVersionsAsync(string ruleGroupCode, CancellationToken ct = default);

    Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(CommissionRule rule, CancellationToken ct = default);
}
