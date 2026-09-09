using FluentAssertions;
using Xunit;
using static Gloria.Domain.UnitTests.EngineTestData;

namespace Gloria.Domain.UnitTests;

public class TieredBracketTests
{
    [Fact]
    public void When_monthly_total_reaches_top_tier_the_highest_rate_applies_to_every_transaction()
    {
        var rule = TieredBracket("ALC");
        var inputs = new[]
        {
            Input(120_000m, productCode: "ALC_FSH", day: 3),
            Input(100_000m, productCode: "ALC_ITL", day: 12),
            Input(80_000m, productCode: "ALC_TRK", day: 20)
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        // monthly total 300,000 -> tier 3 (7%) for all transactions
        trace.Steps.Should().OnlyContain(s => s.AppliedRate == 0.07m);
        trace.TotalCommission.Should().Be(300_000m * 0.07m);
    }

    [Fact]
    public void When_monthly_total_is_in_the_middle_tier_the_middle_rate_applies()
    {
        var rule = TieredBracket("ALC");
        var inputs = new[] { Input(120_000m, day: 5), Input(30_000m, day: 15) }; // total 150,000 -> 5%

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.Steps.Should().OnlyContain(s => s.AppliedRate == 0.05m);
        trace.TotalCommission.Should().Be(150_000m * 0.05m);
    }

    [Fact]
    public void Tier_bound_is_half_open_so_exactly_100000_falls_into_the_second_tier()
    {
        var rule = TieredBracket("ALC");
        var inputs = new[] { Input(100_000m, day: 7) }; // exactly 100,000

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.Steps.Single().AppliedRate.Should().Be(0.05m); // not [0,100k) but [100k,250k)
    }

    [Fact]
    public void Refunds_reduce_the_net_base_and_can_change_the_selected_tier()
    {
        var rule = TieredBracket("ALC");
        var inputs = new[]
        {
            Input(260_000m, day: 4),                 // gross
            Input(20_000m, refund: true, day: 18)    // refund -> net 240,000
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        // net 240,000 -> tier 2 (5%)
        trace.Steps.Should().OnlyContain(s => s.AppliedRate == 0.05m);
        trace.TotalCommission.Should().Be((260_000m - 20_000m) * 0.05m);
        trace.Steps.Single(s => s.IsRefund).StepAmount.Should().Be(-20_000m * 0.05m);
    }
}
