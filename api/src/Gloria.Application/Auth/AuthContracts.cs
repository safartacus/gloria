namespace Gloria.Application.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthUserDto(string Username, string FullName, string RoleCode, int? EmployeeId, bool MustChangePassword);

public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    AuthUserDto User);
