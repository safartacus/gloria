using Gloria.Application.Abstractions;
using Gloria.Domain.Personnel;
using Gloria.Domain.Users;

namespace Gloria.Application.Auth;

/// <summary>
/// Username + password login issuing a short-lived JWT access token and a rotating, server-side
/// refresh token. The access token carries username, full name, role code and employee id.
/// </summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IEmployeeRepository _employees;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users, IRefreshTokenRepository refreshTokens, IEmployeeRepository employees,
        IPasswordHasher passwordHasher, ITokenService tokens, IUnitOfWork uow, IClock clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _employees = employees;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _uow = uow;
        _clock = clock;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username.Trim(), ct);
        if (user is null || !user.IsActive || !user.HasPassword ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash!))
        {
            throw new AuthenticationException("Kullanıcı adı veya şifre hatalı.");
        }

        return await IssueAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var incomingHash = _tokens.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(incomingHash, ct);

        if (stored is null)
            throw new AuthenticationException("Geçersiz oturum. Yeniden giriş yapın.");

        if (!stored.IsActive(now))
        {
            // Reuse of an already-rotated/expired token → revoke everything for that user.
            foreach (var active in await _refreshTokens.GetActiveByUserAsync(stored.UserId, now, ct))
                active.Revoke(now);
            await _uow.SaveChangesAsync(ct);
            throw new AuthenticationException("Oturum süresi doldu. Yeniden giriş yapın.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("Kullanıcı pasif.");

        var (rawNext, hashNext) = _tokens.CreateRefreshToken();
        stored.Revoke(now, hashNext);
        var next = RefreshToken.Issue(user.Id, hashNext, now, _tokens.RefreshTokenLifetime);
        await _refreshTokens.AddAsync(next, ct);
        await _uow.SaveChangesAsync(ct);

        return await BuildResult(user, rawNext, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var stored = await _refreshTokens.GetByHashAsync(_tokens.HashRefreshToken(request.RefreshToken), ct);
        if (stored is null) return;
        stored.Revoke(_clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<AuthResult> ChangePasswordAsync(string username, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user is null || !user.IsActive || !user.HasPassword ||
            !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash!))
        {
            throw new AuthenticationException("Mevcut şifre hatalı.");
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new AuthenticationException("Yeni şifre en az 8 karakter olmalıdır.");
        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash!))
            throw new AuthenticationException("Yeni şifre eskisiyle aynı olamaz.");

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));

        // rotate: revoke every existing refresh token, issue a fresh session
        foreach (var active in await _refreshTokens.GetActiveByUserAsync(user.Id, _clock.UtcNow, ct))
            active.Revoke(_clock.UtcNow);

        return await IssueAsync(user, ct);
    }

    private async Task<AuthResult> IssueAsync(User user, CancellationToken ct)
    {
        var (raw, hash) = _tokens.CreateRefreshToken();
        var token = RefreshToken.Issue(user.Id, hash, _clock.UtcNow, _tokens.RefreshTokenLifetime);
        await _refreshTokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);
        return await BuildResult(user, raw, ct);
    }

    private async Task<AuthResult> BuildResult(User user, string rawRefreshToken, CancellationToken ct)
    {
        var fullName = user.EmployeeId is int empId
            ? (await _employees.GetByIdAsync(empId, ct))?.FullName ?? user.Username
            : user.Username;

        var subject = new TokenSubject(user.Id, user.Username, fullName, user.Role.Code, user.EmployeeId);
        var accessToken = _tokens.CreateAccessToken(subject);

        return new AuthResult(
            accessToken,
            rawRefreshToken,
            (int)_tokens.AccessTokenLifetime.TotalSeconds,
            new AuthUserDto(user.Username, fullName, user.Role.Code, user.EmployeeId, user.MustChangePassword));
    }
}
