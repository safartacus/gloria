using Gloria.Application.Abstractions;
using Gloria.Application.Commissions;
using Gloria.Application.Common;
using Gloria.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/commissions")]
[Authorize]
public sealed class CommissionsController : ControllerBase
{
    private readonly CommissionCalculationService _service;
    private readonly ICurrentUser _currentUser;

    public CommissionsController(CommissionCalculationService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Calculate (or recalculate, if the period is open) commission for a period, optionally one employee.</summary>
    [HttpPost("calculate")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<IReadOnlyList<CommissionResultDto>>> Calculate(
        [FromBody] CommissionCalculationRequest request, CancellationToken ct) =>
        Ok(await _service.CalculateAsync(request, ct));

    /// <summary>
    /// One sorted, paged page of a period's per-employee commission list (no step traces).
    /// <c>sortBy</c>: employeeName | totalCommission | saleCount | refundCount | unmatchedCount | status | calculatedAt.
    /// <c>sortDir</c>: asc | desc (default desc).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<PagedResult<CommissionListItemDto>>> ByPeriod(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default) =>
        Ok(await _service.GetByPeriodPageAsync(new CommissionPageQuery(year, month, page, pageSize, sortBy, sortDir), ct));

    /// <summary>Period-wide aggregate figures (employee count, total commission, total refunds / unmatched).</summary>
    [HttpGet("summary")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<PeriodCommissionSummary>> Summary(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await _service.GetPeriodSummaryAsync(year, month, ct));

    /// <summary>The signed-in employee's own result and full calculation trace.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<CommissionResultDto>> Mine(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (_currentUser.EmployeeId is not int employeeId)
            return Forbid();

        var result = await _service.GetAsync(year, month, employeeId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>One employee's result. Employees may only read their own.</summary>
    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<CommissionResultDto>> ByEmployee(
        int employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var isPrivileged = User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.Accounting);
        if (!isPrivileged && _currentUser.EmployeeId != employeeId)
            return Forbid();

        var result = await _service.GetAsync(year, month, employeeId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
