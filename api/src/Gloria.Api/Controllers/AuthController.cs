using Gloria.Application.Abstractions;
using Gloria.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(AuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    /// <summary>Log in with username + password. Returns a short-lived access token and a rotating refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Exchange a valid refresh token for a new access + refresh token pair (old one is revoked).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Refresh([FromBody] RefreshRequest request, CancellationToken ct) =>
        Ok(await _auth.RefreshAsync(request, ct));

    /// <summary>Revoke the given refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Change the signed-in user's own password. Verifies the current password, clears the
    /// "must change" flag, revokes all existing sessions and returns a fresh token pair.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthResult>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct) =>
        Ok(await _auth.ChangePasswordAsync(_currentUser.Username, request, ct));
}
