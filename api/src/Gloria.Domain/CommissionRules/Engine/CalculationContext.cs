namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// Context for a calculation run that goes beyond a single sale. Tiered-bracket rules pick their
/// tier from the employee's monthly net base for the rule group; that total is pre-aggregated by
/// the engine and read from here.
/// </summary>
public sealed class CalculationContext
{
    private readonly IReadOnlyDictionary<string, decimal> _monthlyNetBaseByRuleGroup;

    public CalculationContext(IReadOnlyDictionary<string, decimal> monthlyNetBaseByRuleGroup)
        => _monthlyNetBaseByRuleGroup = monthlyNetBaseByRuleGroup;

    public decimal MonthlyNetBase(string ruleGroupCode)
        => _monthlyNetBaseByRuleGroup.TryGetValue(ruleGroupCode, out var v) ? v : 0m;
}
