using Gloria.Domain.CommissionRules;
using Gloria.Domain.CommissionRules.Engine;
using Gloria.Domain.Reference;

namespace Gloria.Domain.UnitTests;

/// <summary>Builders for engine tests: rules and inputs.</summary>
internal static class EngineTestData
{
    public static readonly DateOnly PeriodStart = new(2026, 8, 1);

    public static CommissionEngine Engine() => new(new ICommissionRuleHandler[]
    {
        new FixedPercentageHandler(),
        new FixedAmountHandler(),
        new TieredBracketHandler()
    });

    public static RuleType Type(string code) => RuleType.Create(code, code, code, isActive: true);

    public static CommissionRule FixedPercentage(string group, decimal rate, string productPattern = "%", int priority = 100) =>
        CommissionRule.Create(group, $"{group} {rate * 100}%", Type(RuleTypeCodes.FixedPercentage),
            new RuleScope(null, productPattern, null), priority, PeriodStart, "test", DateTime.UtcNow, rate: rate);

    public static CommissionRule FixedAmount(string group, decimal perTransaction, string productPattern = "%", int priority = 100) =>
        CommissionRule.Create(group, $"{group} {perTransaction}TRY", Type(RuleTypeCodes.FixedAmount),
            new RuleScope(null, productPattern, null), priority, PeriodStart, "test", DateTime.UtcNow, amountPerTransaction: perTransaction);

    public static CommissionRule TieredBracket(string group, string productPattern = "%", int priority = 100) =>
        CommissionRule.Create(group, $"{group} tiered", Type(RuleTypeCodes.TieredBracket),
            new RuleScope(null, productPattern, null), priority, PeriodStart, "test", DateTime.UtcNow,
            bracketBasis: BracketBases.MonthlyCumulativeBase,
            bracketTiers: new[]
            {
                new BracketTier(1, 0m, 100_000m, 0.03m),
                new BracketTier(2, 100_000m, 250_000m, 0.05m),
                new BracketTier(3, 250_000m, null, 0.07m)
            });

    private static long _seq;

    public static CommissionInput Input(decimal amount, bool refund = false, string productCode = "SPA_MSJ60",
        string department = "SPA", string source = "PMS", int day = 10) =>
        new(++_seq, source, productCode, department, amount, new DateOnly(2026, 8, day), refund);
}
