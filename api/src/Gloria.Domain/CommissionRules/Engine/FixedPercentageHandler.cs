using Gloria.Domain.Common;
using Gloria.Domain.Reference;

namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>Fixed percentage of the sale amount (e.g. 5%). Refunds contribute negatively.</summary>
public sealed class FixedPercentageHandler : ICommissionRuleHandler
{
    public string HandlerKey => RuleTypeCodes.FixedPercentage;

    public CalculationStep Calculate(CommissionRule rule, CommissionInput input, CalculationContext context)
    {
        var rate = rule.Rate
            ?? throw new DomainException($"Rule '{rule.Name}' has no rate defined.");

        var @base = input.CommissionBase;
        var amount = CommissionMath.Round(@base * rate);

        return new CalculationStep
        {
            SaleRecordId = input.SaleRecordId,
            CommissionRuleId = rule.Id,
            CommissionRuleVersion = rule.Version,
            RuleTypeCode = RuleTypeCodes.FixedPercentage,
            ProductCode = input.ProductCode,
            Base = @base,
            AppliedRate = rate,
            StepAmount = amount,
            IsRefund = input.IsRefund,
            Explanation = $"{input.ProductCode}: {@base:N2} x {rate * 100:0.##}% = {amount:N2}" +
                          (input.IsRefund ? " (refund, deducted)" : "")
        };
    }
}
