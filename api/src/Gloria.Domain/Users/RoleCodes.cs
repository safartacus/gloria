namespace Gloria.Domain.Users;

/// <summary>
/// Known code values in the Role table. Authorization decisions use these constants;
/// no C# enum is used — the table is the source of truth.
/// </summary>
public static class RoleCodes
{
    public const string Admin = "ADMIN";
    public const string Accounting = "ACCOUNTING";
    public const string Employee = "EMPLOYEE";

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, Accounting, Employee };
}
