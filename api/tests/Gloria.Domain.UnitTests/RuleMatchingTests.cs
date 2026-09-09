using FluentAssertions;
using Xunit;
using static Gloria.Domain.UnitTests.EngineTestData;

namespace Gloria.Domain.UnitTests;

public class RuleMatchingTests
{
    [Fact]
    public void When_no_rule_matches_the_transaction_is_counted_as_unmatched_and_contributes_nothing()
    {
        var rule = FixedPercentage("SPA", 0.05m, productPattern: "SPA_%");
        var inputs = new[] { Input(10_000m, productCode: "BUG_DAY") };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.Steps.Should().BeEmpty();
        trace.UnmatchedCount.Should().Be(1);
        trace.TotalCommission.Should().Be(0m);
    }

    [Fact]
    public void When_multiple_rules_match_the_lower_priority_value_wins()
    {
        var general = FixedPercentage("GENERAL", 0.02m, productPattern: "%", priority: 200);
        var special = FixedPercentage("SPA_SPECIAL", 0.08m, productPattern: "SPA_%", priority: 10);
        var inputs = new[] { Input(10_000m, productCode: "SPA_MSJ60") };

        var trace = Engine().Calculate(inputs, new[] { general, special });

        trace.Steps.Single().AppliedRate.Should().Be(0.08m);
        trace.TotalCommission.Should().Be(800m);
    }

    [Fact]
    public void A_rule_not_effective_on_the_transaction_date_is_not_applied()
    {
        var rule = FixedPercentage("SPA", 0.05m);
        rule.Deactivate(new DateOnly(2026, 7, 31)); // not effective in August
        var inputs = new[] { Input(10_000m, day: 10) };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.UnmatchedCount.Should().Be(1);
        trace.TotalCommission.Should().Be(0m);
    }
}
