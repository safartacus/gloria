using Gloria.Domain.Common;
using Gloria.Domain.Reference;

namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>Fixed amount per transaction (e.g. 50 TRY). A refund deducts the same amount.</summary>
public sealed class FixedAmountHandler : ICommissionRuleHandler
{
    public string HandlerKey => RuleTypeCodes.FixedAmount;

    public CalculationStep Calculate(CommissionRule rule, CommissionInput input, CalculationContext context)
    {
        var perTransaction = rule.AmountPerTransaction
            ?? throw new DomainException($"Rule '{rule.Name}' has no per-transaction amount defined.");

        var amount = input.IsRefund ? -perTransaction : perTransaction;

        return new CalculationStep
        {
            SaleRecordId = input.SaleRecordId,
            CommissionRuleId = rule.Id,
            CommissionRuleVersion = rule.Version,
            RuleTypeCode = RuleTypeCodes.FixedAmount,
            ProductCode = input.ProductCode,
            Base = input.CommissionBase,
            AppliedAmount = perTransaction,
            StepAmount = CommissionMath.Round(amount),
            IsRefund = input.IsRefund,
            Explanation = $"{input.ProductCode}: {perTransaction:N2} per transaction" +
                          (input.IsRefund ? " (refund, deducted)" : "")
        };
    }
}
