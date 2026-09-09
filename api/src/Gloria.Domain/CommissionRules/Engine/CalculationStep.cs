namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// The transparent result of a single sale + rule match. "Which sale, which rule, which rate,
/// running total" is answered by the list of these steps.
/// </summary>
public sealed record CalculationStep
{
    public int Order { get; init; }
    public long SaleRecordId { get; init; }
    public int CommissionRuleId { get; init; }
    public int CommissionRuleVersion { get; init; }
    public string RuleTypeCode { get; init; } = default!;
    public string ProductCode { get; init; } = default!;

    /// <summary>The signed base the rate was applied to (negative for refunds).</summary>
    public decimal Base { get; init; }
    public decimal? AppliedRate { get; init; }
    public decimal? AppliedAmount { get; init; }

    /// <summary>This step's commission contribution (negative for refunds).</summary>
    public decimal StepAmount { get; init; }

    /// <summary>Running total including this step.</summary>
    public decimal RunningTotal { get; init; }

    public bool IsRefund { get; init; }
    public string Explanation { get; init; } = default!;
}
