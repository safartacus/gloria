namespace Gloria.Application.Users;

public sealed record CreateUserRequest(string Username, string RoleCode, string? EmployeeNo);

public sealed record UserDto(
    string Username,
    string FullName,
    string RoleCode,
    string? EmployeeNo,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAt);

public sealed record CreatedUserResult(
    string Username,
    string FullName,
    string RoleCode,
    string Password,
    string? CredentialFilePath);
