namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// The calculation behavior of a rule type. <see cref="HandlerKey"/> matches RuleType.HandlerKey.
/// Adding a new rule type = a new implementation of this interface + a row in rule-type.csv.
/// </summary>
public interface ICommissionRuleHandler
{
    string HandlerKey { get; }

    CalculationStep Calculate(CommissionRule rule, CommissionInput input, CalculationContext context);
}
