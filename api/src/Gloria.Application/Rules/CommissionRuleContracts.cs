namespace Gloria.Application.Rules;

public sealed record BracketTierDto(int Order, decimal LowerBound, decimal? UpperBound, decimal Rate);

public sealed record CreateCommissionRuleRequest(
    string RuleGroupCode,
    string Name,
    string RuleTypeCode,
    string? ScopeDepartmentCode,
    string? ScopeProductCodePattern,
    string? ScopeSourceSystemCode,
    int Priority,
    DateOnly EffectiveFrom,
    decimal? Rate,
    decimal? AmountPerTransaction,
    string? BracketBasis,
    IReadOnlyList<BracketTierDto>? BracketTiers);

public sealed record NewVersionRequest(
    DateOnly NewEffectiveFrom,
    string? Name,
    string? ScopeDepartmentCode,
    string? ScopeProductCodePattern,
    string? ScopeSourceSystemCode,
    int? Priority,
    decimal? Rate,
    decimal? AmountPerTransaction,
    string? BracketBasis,
    IReadOnlyList<BracketTierDto>? BracketTiers);

public sealed record CommissionRuleDto(
    int Id,
    string RuleGroupCode,
    int Version,
    string Name,
    string RuleTypeCode,
    string? ScopeDepartmentCode,
    string? ScopeProductCodePattern,
    string? ScopeSourceSystemCode,
    int Priority,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    decimal? Rate,
    decimal? AmountPerTransaction,
    string? BracketBasis,
    IReadOnlyList<BracketTierDto> BracketTiers,
    string CreatedByUsername,
    DateTime CreatedAt);
