using Gloria.Domain.Common;

namespace Gloria.Domain.Calculations;

/// <summary>
/// One row of a persisted calculation trace — the durable counterpart of the engine's CalculationStep.
/// </summary>
public sealed class CommissionResultStep : Entity<long>
{
    public long CommissionResultId { get; private set; }
    public int Order { get; private set; }
    public long SaleRecordId { get; private set; }
    public int CommissionRuleId { get; private set; }
    public int CommissionRuleVersion { get; private set; }
    public string RuleTypeCode { get; private set; } = default!;
    public string ProductCode { get; private set; } = default!;
    public decimal Base { get; private set; }
    public decimal? AppliedRate { get; private set; }
    public decimal? AppliedAmount { get; private set; }
    public decimal StepAmount { get; private set; }
    public decimal RunningTotal { get; private set; }
    public bool IsRefund { get; private set; }
    public string Explanation { get; private set; } = default!;

    private CommissionResultStep() { }

    internal CommissionResultStep(int order, long saleRecordId, int commissionRuleId, int commissionRuleVersion,
        string ruleTypeCode, string productCode, decimal @base, decimal? appliedRate, decimal? appliedAmount,
        decimal stepAmount, decimal runningTotal, bool isRefund, string explanation)
    {
        Order = order;
        SaleRecordId = saleRecordId;
        CommissionRuleId = commissionRuleId;
        CommissionRuleVersion = commissionRuleVersion;
        RuleTypeCode = ruleTypeCode;
        ProductCode = productCode;
        Base = @base;
        AppliedRate = appliedRate;
        AppliedAmount = appliedAmount;
        StepAmount = stepAmount;
        RunningTotal = runningTotal;
        IsRefund = isRefund;
        Explanation = explanation;
    }
}
