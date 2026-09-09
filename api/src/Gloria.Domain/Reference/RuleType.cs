using Gloria.Domain.Common;

namespace Gloria.Domain.Reference;

/// <summary>
/// A commission rule type. Lookup table (data/reference/rule-type.csv). <see cref="HandlerKey"/>
/// resolves to the matching ICommissionRuleHandler in the engine. Adding a new <i>rule</i> only
/// needs a CommissionRule row; adding a new <i>type</i> needs a handler class plus a row here.
/// </summary>
public sealed class RuleType : Entity<int>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string HandlerKey { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private RuleType() { }

    public static RuleType Create(string code, string name, string handlerKey, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Rule type code is required.");
        if (string.IsNullOrWhiteSpace(handlerKey)) throw new DomainException("Handler key is required.");
        return new RuleType
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            HandlerKey = handlerKey.Trim().ToUpperInvariant(),
            IsActive = isActive
        };
    }

    public void Update(string name, string handlerKey, bool isActive)
    {
        Name = name.Trim();
        HandlerKey = handlerKey.Trim().ToUpperInvariant();
        IsActive = isActive;
    }
}

public static class RuleTypeCodes
{
    public const string FixedPercentage = "FIXED_PERCENTAGE";
    public const string TieredBracket = "TIERED_BRACKET";
    public const string FixedAmount = "FIXED_AMOUNT";
}
