using Gloria.Domain.Common;
using Gloria.Domain.CommissionRules.Engine;

namespace Gloria.Domain.Calculations;

public static class CommissionResultStatuses
{
    public const string Draft = "DRAFT";
    public const string Finalized = "FINALIZED";
}

/// <summary>
/// A persisted commission calculation result and its full trace for one employee + period.
/// While the period is open, recalculation refreshes the steps; when the period closes it becomes
/// FINALIZED and immutable.
/// </summary>
public sealed class CommissionResult : AggregateRoot<long>
{
    private readonly List<CommissionResultStep> _steps = new();

    public int PeriodId { get; private set; }
    public int EmployeeId { get; private set; }
    public decimal TotalCommission { get; private set; }
    public int SaleCount { get; private set; }
    public int RefundCount { get; private set; }
    public int UnmatchedCount { get; private set; }
    public string Status { get; private set; } = CommissionResultStatuses.Draft;
    public DateTime CalculatedAt { get; private set; }
    public string CalculatedByUsername { get; private set; } = default!;

    public IReadOnlyList<CommissionResultStep> Steps => _steps;

    private CommissionResult() { }

    public static CommissionResult Create(int periodId, int employeeId, CommissionTrace trace,
        string calculatedByUsername, DateTime now)
    {
        var result = new CommissionResult
        {
            PeriodId = periodId,
            EmployeeId = employeeId,
            CalculatedByUsername = calculatedByUsername
        };
        result.ApplyTrace(trace, now);
        return result;
    }

    /// <summary>Recalculation in an open period: drops current steps and writes the new trace.</summary>
    public void Recalculate(CommissionTrace trace, DateTime now)
    {
        if (Status == CommissionResultStatuses.Finalized)
            throw new DomainException("A finalized result cannot be recalculated (period is closed).");
        ApplyTrace(trace, now);
    }

    public void MarkFinalized() => Status = CommissionResultStatuses.Finalized;

    private void ApplyTrace(CommissionTrace trace, DateTime now)
    {
        _steps.Clear();
        foreach (var s in trace.Steps)
        {
            _steps.Add(new CommissionResultStep(
                s.Order, s.SaleRecordId, s.CommissionRuleId, s.CommissionRuleVersion, s.RuleTypeCode, s.ProductCode,
                s.Base, s.AppliedRate, s.AppliedAmount, s.StepAmount, s.RunningTotal, s.IsRefund, s.Explanation));
        }
        TotalCommission = trace.TotalCommission;
        SaleCount = trace.SaleCount;
        RefundCount = trace.RefundCount;
        UnmatchedCount = trace.UnmatchedCount;
        CalculatedAt = now;
    }
}
