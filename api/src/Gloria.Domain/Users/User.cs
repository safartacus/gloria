using Gloria.Domain.Common;

namespace Gloria.Domain.Users;

/// <summary>
/// A user of the system. Authentication is via JWT: the caller logs in with username + password,
/// and <b>the role is always resolved from this table</b> and embedded in the token. An EMPLOYEE-role
/// user must be linked to an Employee (for row-level access).
/// </summary>
public sealed class User : AggregateRoot<int>
{
    public string Username { get; private set; } = default!;
    public int RoleId { get; private set; }
    public Role Role { get; private set; } = default!;
    public int? EmployeeId { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool MustChangePassword { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string username, Role role, int? employeeId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username is required.");
        if (role.Code == RoleCodes.Employee && employeeId is null)
            throw new DomainException("An EMPLOYEE-role user must be linked to an employee.");
        if (role.Code != RoleCodes.Employee && employeeId is not null)
            throw new DomainException("Only an EMPLOYEE-role user can be linked to an employee.");

        return new User
        {
            Username = username.Trim(),
            RoleId = role.Id,
            Role = role,
            EmployeeId = employeeId,
            IsActive = true,
            CreatedAt = now
        };
    }

    public void ChangeRole(Role newRole, int? employeeId)
    {
        if (newRole.Code == RoleCodes.Employee && employeeId is null)
            throw new DomainException("An EMPLOYEE-role user must be linked to an employee.");
        RoleId = newRole.Id;
        Role = newRole;
        EmployeeId = newRole.Code == RoleCodes.Employee ? employeeId : null;
    }

    /// <summary>Assigns an admin-generated password. The user is flagged to change it after first login.</summary>
    public void SetInitialPasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new DomainException("Password hash is required.");
        PasswordHash = hash;
        MustChangePassword = true;
    }

    /// <summary>The user changed their own password; the "must change" warning is cleared.</summary>
    public void ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash)) throw new DomainException("Password hash is required.");
        PasswordHash = newHash;
        MustChangePassword = false;
    }

    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
