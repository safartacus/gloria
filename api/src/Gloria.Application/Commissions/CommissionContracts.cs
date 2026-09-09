namespace Gloria.Application.Commissions;

public sealed record CommissionCalculationRequest(int Year, int Month, int? EmployeeId);

/// <summary>Server-side paging + sorting query for the period commission list.</summary>
public sealed record CommissionPageQuery(int Year, int Month, int Page, int PageSize, string? SortBy, string? SortDir);

/// <summary>One row of the period commission list — no calculation steps (fetch detail separately).</summary>
public sealed record CommissionListItemDto(
    int PeriodId,
    int Year,
    int Month,
    string PeriodStatus,
    int EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    decimal TotalCommission,
    int SaleCount,
    int RefundCount,
    int UnmatchedCount,
    string Status,
    DateTime CalculatedAt,
    string CalculatedByUsername);

public sealed record PeriodCommissionSummary(
    int EmployeeCount,
    decimal TotalCommission,
    int TotalRefunds,
    int TotalUnmatched);

public sealed record CalculationStepDto(
    int Order,
    long SaleRecordId,
    int CommissionRuleId,
    int CommissionRuleVersion,
    string RuleTypeCode,
    string ProductCode,
    decimal Base,
    decimal? AppliedRate,
    decimal? AppliedAmount,
    decimal StepAmount,
    decimal RunningTotal,
    bool IsRefund,
    string Explanation);

public sealed record CommissionResultDto(
    int PeriodId,
    int Year,
    int Month,
    string PeriodStatus,
    int EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    decimal TotalCommission,
    int SaleCount,
    int RefundCount,
    int UnmatchedCount,
    string Status,
    DateTime CalculatedAt,
    string CalculatedByUsername,
    IReadOnlyList<CalculationStepDto> Steps);
