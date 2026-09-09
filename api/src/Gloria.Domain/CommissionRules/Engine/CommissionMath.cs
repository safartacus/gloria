namespace Gloria.Domain.CommissionRules.Engine;

public static class CommissionMath
{
    /// <summary>Monetary rounding: 2 decimals, half away from zero. Every step uses the same rule.</summary>
    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
