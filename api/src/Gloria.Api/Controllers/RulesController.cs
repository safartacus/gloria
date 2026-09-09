using Gloria.Application.Rules;
using Gloria.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/rules")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
public sealed class RulesController : ControllerBase
{
    private readonly CommissionRuleService _rules;

    public RulesController(CommissionRuleService rules) => _rules = rules;

    /// <summary>All commission rules (every version).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleDto>>> List(CancellationToken ct) =>
        Ok(await _rules.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommissionRuleDto>> Get(int id, CancellationToken ct)
    {
        var rule = await _rules.GetAsync(id, ct);
        return rule is null ? NotFound() : Ok(rule);
    }

    /// <summary>Create a new rule. No code change is needed for a new rule or a new rate.</summary>
    [HttpPost]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<ActionResult<CommissionRuleDto>> Create([FromBody] CreateCommissionRuleRequest request, CancellationToken ct)
    {
        var created = await _rules.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Open a new version of an existing rule (closes the current one from the given date).</summary>
    [HttpPost("{id:int}/versions")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<ActionResult<CommissionRuleDto>> NewVersion(int id, [FromBody] NewVersionRequest request, CancellationToken ct) =>
        Ok(await _rules.CreateNewVersionAsync(id, request, ct));

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<ActionResult<CommissionRuleDto>> Deactivate(int id, [FromQuery] DateOnly effectiveTo, CancellationToken ct) =>
        Ok(await _rules.DeactivateAsync(id, effectiveTo, ct));
}
