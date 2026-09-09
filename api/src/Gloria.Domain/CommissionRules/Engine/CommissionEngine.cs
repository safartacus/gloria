using Gloria.Domain.Common;

namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// Pure (infrastructure-free) commission engine. Takes an employee's sales for a period plus the
/// rules effective in that period, matches each sale to a single rule (priority + effective date),
/// invokes the type's handler, and produces a transparent trace.
/// </summary>
public sealed class CommissionEngine
{
    private readonly IReadOnlyDictionary<string, ICommissionRuleHandler> _handlers;

    public CommissionEngine(IEnumerable<ICommissionRuleHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.HandlerKey, StringComparer.OrdinalIgnoreCase);
    }

    public CommissionTrace Calculate(
        IReadOnlyCollection<CommissionInput> inputs,
        IReadOnlyCollection<CommissionRule> rules)
    {
        // 1) each input -> the single best-matching rule
        var matches = new List<(CommissionInput Input, CommissionRule Rule)>();
        var unmatched = 0;

        foreach (var input in inputs)
        {
            var rule = rules
                .Where(r => r.IsEffectiveOn(input.TransactionDate) && r.Scope.Matches(input))
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.Version)
                .FirstOrDefault();

            if (rule is null) { unmatched++; continue; }
            matches.Add((input, rule));
        }

        // 2) pre-aggregate monthly net base per rule group (for tiered-bracket rules)
        var monthlyNetBase = matches
            .GroupBy(m => m.Rule.RuleGroupCode)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Input.CommissionBase));
        var context = new CalculationContext(monthlyNetBase);

        // 3) step-by-step calculation in deterministic order
        var steps = new List<CalculationStep>(matches.Count);
        var order = 1;
        var runningTotal = 0m;

        foreach (var (input, rule) in matches
                     .OrderBy(m => m.Input.TransactionDate)
                     .ThenBy(m => m.Input.SaleRecordId))
        {
            if (!_handlers.TryGetValue(rule.RuleType.HandlerKey, out var handler))
                throw new DomainException(
                    $"No ICommissionRuleHandler registered for handler key '{rule.RuleType.HandlerKey}'.");

            var step = handler.Calculate(rule, input, context);
            runningTotal = CommissionMath.Round(runningTotal + step.StepAmount);
            steps.Add(step with { Order = order++, RunningTotal = runningTotal });
        }

        return new CommissionTrace(
            steps,
            TotalCommission: runningTotal,
            SaleCount: matches.Count(m => !m.Input.IsRefund),
            RefundCount: matches.Count(m => m.Input.IsRefund),
            UnmatchedCount: unmatched);
    }
}
