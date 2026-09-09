using Gloria.Application.Periods;
using Gloria.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/periods")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
public sealed class PeriodsController : ControllerBase
{
    private readonly PeriodService _periods;

    public PeriodsController(PeriodService periods) => _periods = periods;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PeriodDto>>> List(CancellationToken ct) =>
        Ok(await _periods.ListAsync(ct));

    /// <summary>Open a period (also happens automatically on first calculation).</summary>
    [HttpPost("open")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<ActionResult<PeriodDto>> Open([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await _periods.OpenAsync(year, month, ct));

    /// <summary>Close a period. After closing, its sales / rules / results cannot be changed.</summary>
    [HttpPost("close")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<ActionResult<PeriodDto>> Close([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await _periods.CloseAsync(year, month, ct));
}
