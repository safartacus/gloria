using Gloria.Domain.Common;
using Gloria.Domain.Reference;

namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// Tiered bracket: whichever tier the employee's monthly cumulative net base for the rule group
/// falls into, that tier's rate is applied to this transaction's base ("once the monthly target is
/// exceeded, the rate goes up"). If the monthly total is above the top tier, the highest rate applies.
/// </summary>
public sealed class TieredBracketHandler : ICommissionRuleHandler
{
    public string HandlerKey => RuleTypeCodes.TieredBracket;

    public CalculationStep Calculate(CommissionRule rule, CommissionInput input, CalculationContext context)
    {
        if (rule.BracketTiers.Count == 0)
            throw new DomainException($"Rule '{rule.Name}' has no bracket tiers defined.");

        var tiers = rule.BracketTiers.OrderBy(t => t.Order).ToList();

        var basisValue = rule.BracketBasis == BracketBases.TransactionAmount
            ? Math.Abs(input.Amount)
            : context.MonthlyNetBase(rule.RuleGroupCode);

        var tier = tiers.FirstOrDefault(t => t.Contains(basisValue)) ?? tiers[^1];

        var @base = input.CommissionBase;
        var amount = CommissionMath.Round(@base * tier.Rate);

        return new CalculationStep
        {
            SaleRecordId = input.SaleRecordId,
            CommissionRuleId = rule.Id,
            CommissionRuleVersion = rule.Version,
            RuleTypeCode = RuleTypeCodes.TieredBracket,
            ProductCode = input.ProductCode,
            Base = @base,
            AppliedRate = tier.Rate,
            StepAmount = amount,
            IsRefund = input.IsRefund,
            Explanation =
                $"{input.ProductCode}: bracket basis {basisValue:N2} -> tier {tier.Order} ({tier.Rate * 100:0.##}%); " +
                $"{@base:N2} x {tier.Rate * 100:0.##}% = {amount:N2}" + (input.IsRefund ? " (refund, deducted)" : "")
        };
    }
}
