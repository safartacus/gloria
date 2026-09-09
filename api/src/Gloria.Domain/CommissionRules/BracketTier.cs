using Gloria.Domain.Common;

namespace Gloria.Domain.CommissionRules;

/// <summary>
/// One step of a tiered-bracket rule. [LowerBound, UpperBound) is a half-open interval;
/// a null UpperBound means "and above".
/// </summary>
public sealed class BracketTier : Entity<int>
{
    public int CommissionRuleId { get; private set; }
    public int Order { get; private set; }
    public decimal LowerBound { get; private set; }
    public decimal? UpperBound { get; private set; }
    public decimal Rate { get; private set; }

    private BracketTier() { }

    public BracketTier(int order, decimal lowerBound, decimal? upperBound, decimal rate)
    {
        if (order < 1) throw new DomainException("Tier order cannot be less than 1.");
        if (lowerBound < 0) throw new DomainException("Tier lower bound cannot be negative.");
        if (upperBound is not null && upperBound <= lowerBound)
            throw new DomainException("Tier upper bound must be greater than the lower bound.");
        if (rate is < 0 or > 1) throw new DomainException("Tier rate must be within 0-1 (e.g. 0.05).");

        Order = order;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        Rate = rate;
    }

    public bool Contains(decimal value) => value >= LowerBound && (UpperBound is null || value < UpperBound);
}
