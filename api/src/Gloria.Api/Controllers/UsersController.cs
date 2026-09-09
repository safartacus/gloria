using Gloria.Application.Personnel;
using Gloria.Application.Users;
using Gloria.Domain.Users;
using Gloria.Infrastructure.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleCodes.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly UserAdminService _users;
    private readonly PersonnelSyncService _personnelSync;
    private readonly PersonnelSyncSource _personnelSource;

    public UsersController(UserAdminService users, PersonnelSyncService personnelSync, PersonnelSyncSource personnelSource)
    {
        _users = users;
        _personnelSync = personnelSync;
        _personnelSource = personnelSource;
    }

    /// <summary>All user accounts (role, linked employee, active state, password-change flag).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) =>
        Ok(await _users.ListAsync(ct));

    /// <summary>
    /// Create a user. A random password is generated, hashed, written to a credential note, and
    /// returned once in the response so the admin can hand it over. The user must change it at first login.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreatedUserResult>> Create([FromBody] CreateUserRequest request, CancellationToken ct) =>
        Ok(await _users.CreateAsync(request, ct));

    /// <summary>
    /// Additive roster sync: reads <c>data/personel.csv</c> and creates any employee (and its
    /// EMPLOYEE-role login) that is not already in the database. Existing accounts are untouched.
    /// The same job also runs nightly. Generated passwords are returned once.
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<PersonnelSyncResult>> Sync(CancellationToken ct)
    {
        if (!_personnelSource.Exists)
            return BadRequest(new { title = "Dosya yok", detail = $"personel.csv bulunamadı: {_personnelSource.ResolvedPath}" });

        await using var stream = _personnelSource.Open();
        return Ok(await _personnelSync.SyncAsync(stream, ct));
    }

    [HttpPost("{username}/deactivate")]
    public async Task<IActionResult> Deactivate(string username, CancellationToken ct)
    {
        await _users.SetActiveAsync(username, active: false, ct);
        return NoContent();
    }

    [HttpPost("{username}/activate")]
    public async Task<IActionResult> Activate(string username, CancellationToken ct)
    {
        await _users.SetActiveAsync(username, active: true, ct);
        return NoContent();
    }
}
