using FluentAssertions;
using Xunit;
using static Gloria.Domain.UnitTests.EngineTestData;

namespace Gloria.Domain.UnitTests;

public class RefundTests
{
    [Fact]
    public void Fixed_percentage_refund_is_deducted_from_the_commission()
    {
        var rule = FixedPercentage("SPA", 0.05m);
        var inputs = new[]
        {
            Input(10_000m, day: 5),
            Input(4_000m, refund: true, day: 22)
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.TotalCommission.Should().Be(10_000m * 0.05m - 4_000m * 0.05m); // 500 - 200 = 300
        trace.Steps.Single(s => s.IsRefund).StepAmount.Should().Be(-200m);
        trace.SaleCount.Should().Be(1);
        trace.RefundCount.Should().Be(1);
    }

    [Fact]
    public void Fixed_amount_refund_deducts_the_per_transaction_amount()
    {
        var rule = FixedAmount("BUGGY", 50m);
        var inputs = new[]
        {
            Input(4_500m, productCode: "BUG_DAY", day: 2),
            Input(4_500m, productCode: "BUG_DAY", day: 9),
            Input(4_500m, productCode: "BUG_DAY", day: 16),
            Input(4_500m, productCode: "BUG_DAY", refund: true, day: 28)
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.TotalCommission.Should().Be(3 * 50m - 50m); // 100
        trace.Steps.Single(s => s.IsRefund).StepAmount.Should().Be(-50m);
    }

    [Fact]
    public void Commission_can_go_negative_when_refunds_exceed_sales()
    {
        var rule = FixedPercentage("SPA", 0.10m);
        var inputs = new[]
        {
            Input(1_000m, day: 3),
            Input(5_000m, refund: true, day: 20)
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.TotalCommission.Should().Be(100m - 500m); // -400
    }

    [Fact]
    public void Running_total_reflects_the_accumulated_sum_at_each_step()
    {
        var rule = FixedPercentage("SPA", 0.05m);
        var inputs = new[]
        {
            Input(10_000m, day: 1),
            Input(2_000m, refund: true, day: 2),
            Input(6_000m, day: 3)
        };

        var trace = Engine().Calculate(inputs, new[] { rule });

        trace.Steps.Select(s => s.RunningTotal).Should().ContainInOrder(500m, 400m, 700m);
        trace.TotalCommission.Should().Be(700m);
    }
}
