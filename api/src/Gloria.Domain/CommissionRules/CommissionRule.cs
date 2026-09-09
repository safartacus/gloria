using Gloria.Domain.Common;
using Gloria.Domain.Reference;

namespace Gloria.Domain.CommissionRules;

public static class BracketBases
{
    /// <summary>Tier is chosen by the employee's monthly cumulative net base for the rule group.</summary>
    public const string MonthlyCumulativeBase = "MONTHLY_CUMULATIVE_BASE";

    /// <summary>Tier is chosen by the individual transaction amount.</summary>
    public const string TransactionAmount = "TRANSACTION_AMOUNT";
}

/// <summary>
/// A versioned commission rule defined in the database. A new rule or a rate change needs no code —
/// only a new row here. Rules are never updated: the current version is closed via
/// <see cref="EffectiveTo"/> and a new version is opened, so past periods stay exactly reproducible.
/// </summary>
public sealed class CommissionRule : AggregateRoot<int>, IAuditable
{
    private readonly List<BracketTier> _bracketTiers = new();

    public string RuleGroupCode { get; private set; } = default!;
    public int Version { get; private set; }
    public string Name { get; private set; } = default!;

    public int RuleTypeId { get; private set; }
    public RuleType RuleType { get; private set; } = default!;

    public RuleScope Scope { get; private set; } = default!;
    public int Priority { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    // --- Parameters used per type (plain columns instead of enum/JSON) ---
    /// <summary>Rate for FIXED_PERCENTAGE (e.g. 0.05).</summary>
    public decimal? Rate { get; private set; }
    /// <summary>Per-transaction amount for FIXED_AMOUNT (e.g. 50).</summary>
    public decimal? AmountPerTransaction { get; private set; }
    /// <summary>Bracket basis for TIERED_BRACKET: MONTHLY_CUMULATIVE_BASE | TRANSACTION_AMOUNT.</summary>
    public string? BracketBasis { get; private set; }

    public IReadOnlyList<BracketTier> BracketTiers => _bracketTiers;

    public string CreatedByUsername { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private CommissionRule() { }

    public static CommissionRule Create(
        string ruleGroupCode,
        string name,
        RuleType ruleType,
        RuleScope scope,
        int priority,
        DateOnly effectiveFrom,
        string createdByUsername,
        DateTime now,
        decimal? rate = null,
        decimal? amountPerTransaction = null,
        string? bracketBasis = null,
        IEnumerable<BracketTier>? bracketTiers = null)
    {
        if (string.IsNullOrWhiteSpace(ruleGroupCode)) throw new DomainException("Rule group code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Rule name is required.");
        if (!ruleType.IsActive) throw new DomainException($"Rule type '{ruleType.Code}' is inactive.");

        var rule = new CommissionRule
        {
            RuleGroupCode = ruleGroupCode.Trim().ToUpperInvariant(),
            Version = 1,
            Name = name.Trim(),
            RuleTypeId = ruleType.Id,
            RuleType = ruleType,
            Scope = scope,
            Priority = priority,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            CreatedByUsername = createdByUsername,
            CreatedAt = now
        };
        rule.AssignParameters(ruleType.Code, rate, amountPerTransaction, bracketBasis, bracketTiers);
        return rule;
    }

    /// <summary>
    /// Closes this version from the given date and returns a new continuing version.
    /// </summary>
    public CommissionRule CreateNewVersion(
        DateOnly newEffectiveFrom,
        string createdByUsername,
        DateTime now,
        string? name = null,
        RuleScope? scope = null,
        int? priority = null,
        decimal? rate = null,
        decimal? amountPerTransaction = null,
        string? bracketBasis = null,
        IEnumerable<BracketTier>? bracketTiers = null)
    {
        if (newEffectiveFrom <= EffectiveFrom)
            throw new DomainException("The new version must start after the current version's start date.");

        EffectiveTo = newEffectiveFrom.AddDays(-1);
        IsActive = false;

        var next = new CommissionRule
        {
            RuleGroupCode = RuleGroupCode,
            Version = Version + 1,
            Name = (name ?? Name).Trim(),
            RuleTypeId = RuleTypeId,
            RuleType = RuleType,
            Scope = scope ?? Scope,
            Priority = priority ?? Priority,
            EffectiveFrom = newEffectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            CreatedByUsername = createdByUsername,
            CreatedAt = now
        };
        next.AssignParameters(
            RuleType.Code,
            rate ?? Rate,
            amountPerTransaction ?? AmountPerTransaction,
            bracketBasis ?? BracketBasis,
            bracketTiers ?? _bracketTiers.Select(t => new BracketTier(t.Order, t.LowerBound, t.UpperBound, t.Rate)));
        return next;
    }

    public void Deactivate(DateOnly effectiveTo)
    {
        IsActive = false;
        EffectiveTo ??= effectiveTo;
    }

    /// <summary>
    /// A closed version (EffectiveTo set) still applies within its historical window even after being
    /// superseded; an open-ended version applies only while active. This keeps mid-month versioning correct.
    /// </summary>
    public bool IsEffectiveOn(DateOnly date) =>
        date >= EffectiveFrom
        && (EffectiveTo is null ? IsActive : date <= EffectiveTo);

    private void AssignParameters(
        string ruleTypeCode,
        decimal? rate,
        decimal? amountPerTransaction,
        string? bracketBasis,
        IEnumerable<BracketTier>? bracketTiers)
    {
        _bracketTiers.Clear();
        Rate = null;
        AmountPerTransaction = null;
        BracketBasis = null;

        switch (ruleTypeCode)
        {
            case RuleTypeCodes.FixedPercentage:
                if (rate is null or <= 0 or > 1)
                    throw new DomainException("A FIXED_PERCENTAGE rule needs a rate within 0-1 (e.g. 0.05).");
                Rate = rate;
                break;

            case RuleTypeCodes.FixedAmount:
                if (amountPerTransaction is null or <= 0)
                    throw new DomainException("A FIXED_AMOUNT rule needs a positive per-transaction amount.");
                AmountPerTransaction = amountPerTransaction;
                break;

            case RuleTypeCodes.TieredBracket:
                var tiers = (bracketTiers ?? Enumerable.Empty<BracketTier>())
                    .OrderBy(t => t.Order).ToList();
                if (tiers.Count == 0)
                    throw new DomainException("A TIERED_BRACKET rule needs at least one tier.");
                ValidateTiers(tiers);
                BracketBasis = string.IsNullOrWhiteSpace(bracketBasis)
                    ? BracketBases.MonthlyCumulativeBase
                    : bracketBasis.Trim().ToUpperInvariant();
                _bracketTiers.AddRange(tiers);
                break;

            default:
                throw new DomainException($"Unknown rule type code: {ruleTypeCode}");
        }
    }

    private static void ValidateTiers(IReadOnlyList<BracketTier> tiers)
    {
        for (var i = 0; i < tiers.Count; i++)
        {
            if (i == 0 && tiers[i].LowerBound != 0)
                throw new DomainException("The first tier's lower bound must be 0.");
            if (i > 0 && tiers[i - 1].UpperBound != tiers[i].LowerBound)
                throw new DomainException("Tiers must be contiguous (previous upper bound = next lower bound).");
            if (i < tiers.Count - 1 && tiers[i].UpperBound is null)
                throw new DomainException("Only the last tier may have a null (infinite) upper bound.");
        }
    }
}
