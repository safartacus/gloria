using Gloria.Application.Abstractions;
using Gloria.Domain.CommissionRules;
using Gloria.Domain.Common;
using Gloria.Domain.Reference;

namespace Gloria.Application.Rules;

/// <summary>
/// Create / list / version commission rules. A new rule or a changed rate is data only — no code
/// change and no deployment. Editing a rule is modeled as opening a new version so history stays
/// reproducible.
/// </summary>
public sealed class CommissionRuleService
{
    private readonly ICommissionRuleRepository _rules;
    private readonly IRuleTypeRepository _ruleTypes;
    private readonly ICacheStore _cache;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CommissionRuleService(ICommissionRuleRepository rules, IRuleTypeRepository ruleTypes, ICacheStore cache,
        IUnitOfWork uow, ICurrentUser currentUser, IClock clock)
    {
        _rules = rules;
        _ruleTypes = ruleTypes;
        _cache = cache;
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CommissionRuleDto>> ListAsync(CancellationToken ct = default)
    {
        var gen = await _cache.GetGenerationAsync(CacheKeys.RulesGen, ct);
        return await _cache.GetOrCreateAsync(
            CacheKeys.RulesList(gen), CacheKeys.RuleTtl,
            async () => (await _rules.GetAllAsync(ct)).Select(ToDto).ToList(),
            ct);
    }

    private async Task InvalidateAsync(CancellationToken ct) => await _cache.BumpGenerationAsync(CacheKeys.RulesGen, ct);

    public async Task<CommissionRuleDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(id, ct);
        return rule is null ? null : ToDto(rule);
    }

    public async Task<CommissionRuleDto> CreateAsync(CreateCommissionRuleRequest request, CancellationToken ct = default)
    {
        var ruleType = await _ruleTypes.GetByCodeAsync(request.RuleTypeCode, ct)
            ?? throw new DomainException($"Rule type '{request.RuleTypeCode}' not found.");

        var rule = CommissionRule.Create(
            request.RuleGroupCode,
            request.Name,
            ruleType,
            new RuleScope(request.ScopeDepartmentCode, request.ScopeProductCodePattern, request.ScopeSourceSystemCode),
            request.Priority,
            request.EffectiveFrom,
            _currentUser.Username,
            _clock.UtcNow,
            request.Rate,
            request.AmountPerTransaction,
            request.BracketBasis,
            MapTiers(request.BracketTiers));

        await _rules.AddAsync(rule, ct);
        await _uow.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
        return ToDto(rule);
    }

    public async Task<CommissionRuleDto> CreateNewVersionAsync(int ruleId, NewVersionRequest request, CancellationToken ct = default)
    {
        var current = await _rules.GetByIdAsync(ruleId, ct)
            ?? throw new DomainException($"Commission rule {ruleId} not found.");

        RuleScope? scope = request.ScopeDepartmentCode is null && request.ScopeProductCodePattern is null && request.ScopeSourceSystemCode is null
            ? null
            : new RuleScope(request.ScopeDepartmentCode, request.ScopeProductCodePattern, request.ScopeSourceSystemCode);

        var next = current.CreateNewVersion(
            request.NewEffectiveFrom,
            _currentUser.Username,
            _clock.UtcNow,
            request.Name,
            scope,
            request.Priority,
            request.Rate,
            request.AmountPerTransaction,
            request.BracketBasis,
            MapTiers(request.BracketTiers));

        await _rules.AddAsync(next, ct);
        await _uow.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
        return ToDto(next);
    }

    public async Task<CommissionRuleDto> DeactivateAsync(int ruleId, DateOnly effectiveTo, CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(ruleId, ct)
            ?? throw new DomainException($"Commission rule {ruleId} not found.");
        rule.Deactivate(effectiveTo);
        await _uow.SaveChangesAsync(ct);
        await InvalidateAsync(ct);
        return ToDto(rule);
    }

    private static IEnumerable<BracketTier>? MapTiers(IReadOnlyList<BracketTierDto>? tiers) =>
        tiers?.Select(t => new BracketTier(t.Order, t.LowerBound, t.UpperBound, t.Rate));

    private static CommissionRuleDto ToDto(CommissionRule r) => new(
        r.Id, r.RuleGroupCode, r.Version, r.Name, r.RuleType.Code,
        r.Scope.DepartmentCode, r.Scope.ProductCodePattern, r.Scope.SourceSystemCode,
        r.Priority, r.EffectiveFrom, r.EffectiveTo, r.IsActive,
        r.Rate, r.AmountPerTransaction, r.BracketBasis,
        r.BracketTiers.OrderBy(t => t.Order).Select(t => new BracketTierDto(t.Order, t.LowerBound, t.UpperBound, t.Rate)).ToList(),
        r.CreatedByUsername, r.CreatedAt);
}
