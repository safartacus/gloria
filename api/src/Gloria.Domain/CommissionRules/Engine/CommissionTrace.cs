namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// The full result the engine produces for one employee + period: a step-by-step trace plus totals.
/// </summary>
public sealed record CommissionTrace(
    IReadOnlyList<CalculationStep> Steps,
    decimal TotalCommission,
    int SaleCount,
    int RefundCount,
    int UnmatchedCount);
