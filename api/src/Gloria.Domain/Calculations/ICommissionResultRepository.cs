namespace Gloria.Domain.Calculations;

/// <summary>Aggregate figures for a whole period, computed in the database.</summary>
public readonly record struct PeriodCommissionTotals(int EmployeeCount, decimal TotalCommission, int RefundCount, int UnmatchedCount);

public interface ICommissionResultRepository
{
    Task<CommissionResult?> GetAsync(int periodId, int employeeId, CancellationToken ct = default);

    Task<IReadOnlyList<CommissionResult>> GetByPeriodAsync(int periodId, CancellationToken ct = default);

    /// <summary>
    /// One page of the period's results, sorted in the database. Steps are NOT loaded (list view).
    /// <paramref name="sortBy"/>: employeeName | totalCommission | saleCount | refundCount |
    /// unmatchedCount | status | calculatedAt (default: totalCommission).
    /// </summary>
    Task<(IReadOnlyList<CommissionResult> Items, int TotalCount)> GetByPeriodPageAsync(
        int periodId, int skip, int take, string? sortBy, bool descending, CancellationToken ct = default);

    Task<PeriodCommissionTotals> GetPeriodTotalsAsync(int periodId, CancellationToken ct = default);

    Task AddAsync(CommissionResult result, CancellationToken ct = default);
}
