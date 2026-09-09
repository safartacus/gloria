using Gloria.Domain.Common;

namespace Gloria.Domain.Users;

/// <summary>
/// Authorization role. A table rather than a code-level enum; its rows are loaded from the
/// reference CSV (data/reference/role.csv). Compared against <see cref="RoleCodes"/> constants.
/// </summary>
public sealed class Role : Entity<int>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private Role() { }

    public static Role Create(string code, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Role code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name is required.");
        return new Role { Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), Description = description?.Trim() };
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }
}
