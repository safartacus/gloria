using Gloria.Application.Abstractions;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class LookupController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRuleTypeRepository _ruleTypes;
    private readonly ISourceSystemRepository _sourceSystems;
    private readonly IDepartmentRepository _departments;
    private readonly IEmployeeRepository _employees;

    public LookupController(ICurrentUser currentUser, IUserRepository users, IRoleRepository roles,
        IRuleTypeRepository ruleTypes, ISourceSystemRepository sourceSystems, IDepartmentRepository departments,
        IEmployeeRepository employees)
    {
        _currentUser = currentUser;
        _users = users;
        _roles = roles;
        _ruleTypes = ruleTypes;
        _sourceSystems = sourceSystems;
        _departments = departments;
        _employees = employees;
    }

    /// <summary>The signed-in user: username, full name, role, linked employee, password-change flag.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(_currentUser.Username, ct);
        return Ok(new
        {
            _currentUser.Username,
            _currentUser.FullName,
            _currentUser.RoleCode,
            _currentUser.EmployeeId,
            MustChangePassword = user?.MustChangePassword ?? false,
        });
    }

    [HttpGet("roles")]
    public async Task<ActionResult<object>> Roles(CancellationToken ct) =>
        Ok((await _roles.GetAllAsync(ct)).Select(r => new { r.Code, r.Name, r.Description }));

    [HttpGet("rule-types")]
    public async Task<ActionResult<object>> RuleTypes(CancellationToken ct) =>
        Ok((await _ruleTypes.GetAllAsync(ct)).Select(t => new { t.Code, t.Name, t.HandlerKey, t.IsActive }));

    [HttpGet("source-systems")]
    public async Task<ActionResult<object>> SourceSystems(CancellationToken ct) =>
        Ok((await _sourceSystems.GetAllAsync(ct)).Select(s => new { s.Code, s.Name }));

    [HttpGet("departments")]
    public async Task<ActionResult<object>> Departments(CancellationToken ct) =>
        Ok((await _departments.GetAllAsync(ct)).Select(d => new { d.Id, d.Code, d.Name }));

    [HttpGet("employees")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<object>> Employees(CancellationToken ct) =>
        Ok((await _employees.GetAllAsync(ct)).Select(e => new { e.Id, e.EmployeeNo, e.FullName, e.DepartmentId, e.Hotel }));
}
