namespace Gloria.Infrastructure.Persistence.Seeding;

/// <summary>
/// Reference / master data seeded straight into the database via EF Core migrations (HasData).
/// There are no CSV files to place or tamper with; a fresh <c>database update</c> produces a working
/// system. Every seeded user shares the default password below and is flagged
/// <c>MustChangePassword</c>, so the first login forces a change.
/// </summary>
internal static class SeedData
{
    public static readonly DateTime SeededAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Plain default password for every seeded account: <c>Gloria2026!</c></summary>
    public const string DefaultPassword = "Gloria2026!";

    /// <summary>PBKDF2-SHA256 hash of <see cref="DefaultPassword"/> (see Pbkdf2PasswordHasher format).</summary>
    public const string DefaultPasswordHash =
        "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=";

    public static readonly object[] Roles =
    {
        new { Id = 1, Code = "ADMIN", Name = "Administrator", Description = (string?)"Manages commission rules, periods and imports" },
        new { Id = 2, Code = "ACCOUNTING", Name = "Accounting", Description = (string?)"Sees every employee's commission" },
        new { Id = 3, Code = "EMPLOYEE", Name = "Employee", Description = (string?)"Sees only their own commission" },
    };

    public static readonly object[] SourceSystems =
    {
        new { Id = 1, Code = "PMS", Name = "Fidelio Property Management System" },
        new { Id = 2, Code = "POS", Name = "Flyby Point of Sale" },
        new { Id = 3, Code = "ERP", Name = "Oracle JD Edwards" },
    };

    public static readonly object[] RuleTypes =
    {
        new { Id = 1, Code = "FIXED_PERCENTAGE", Name = "Fixed percentage of sale amount", HandlerKey = "FIXED_PERCENTAGE", IsActive = true },
        new { Id = 2, Code = "TIERED_BRACKET", Name = "Tiered bracket on monthly cumulative base", HandlerKey = "TIERED_BRACKET", IsActive = true },
        new { Id = 3, Code = "FIXED_AMOUNT", Name = "Fixed amount per transaction", HandlerKey = "FIXED_AMOUNT", IsActive = true },
    };

    public static readonly object[] Departments =
    {
        new { Id = 1, Code = "SPA", Name = "SPA", IsActive = true },
        new { Id = 2, Code = "F&B", Name = "F&B", IsActive = true },
        new { Id = 3, Code = "ÖN BÜRO", Name = "ÖN BÜRO", IsActive = true },
        new { Id = 4, Code = "GUEST RELATIONS", Name = "GUEST RELATIONS", IsActive = true },
    };

    public static readonly object[] Employees =
    {
        Emp(1, "P1001", "Ayşe Demir", 1, "GSR", 2024, 3, 1),
        Emp(2, "P1002", "Mehmet Kaya", 1, "GSR", 2024, 3, 1),
        Emp(3, "P1003", "Elif Şahin", 1, "GGR", 2024, 3, 1),
        Emp(4, "P1004", "Burak Yıldız", 2, "GSR", 2024, 3, 1),
        Emp(5, "P1005", "Zeynep Arslan", 2, "GGR", 2024, 3, 1),
        Emp(6, "P1006", "Can Öztürk", 2, "GVR", 2024, 3, 1),
        Emp(7, "P1007", "Selin Çelik", 3, "GSR", 2024, 3, 1),
        Emp(8, "P1008", "Emre Koç", 3, "GGR", 2024, 3, 1),
        Emp(9, "P1009", "Deniz Aydın", 4, "GSR", 2024, 3, 1),
        Emp(10, "P1010", "Merve Polat", 4, "GVR", 2024, 3, 1),
        Emp(11, "P1011", "Okan Doğan", 1, "GVR", 2024, 3, 1),
        Emp(12, "P1012", "Gamze Kurt", 2, "GSR", 2026, 8, 18),
        Emp(13, "P1013", "Hakan Ateş", 1, "GSR", 2023, 5, 15, new DateOnly(2026, 8, 12)),
    };

    public static readonly object[] Users =
    {
        User(1, "P1001", roleId: 3, employeeId: 1),
        User(2, "P1002", roleId: 3, employeeId: 2),
        User(3, "P1003", roleId: 3, employeeId: 3),
        User(4, "P1004", roleId: 3, employeeId: 4),
        User(5, "P1005", roleId: 3, employeeId: 5),
        User(6, "P1006", roleId: 3, employeeId: 6),
        User(7, "P1007", roleId: 3, employeeId: 7),
        User(8, "P1008", roleId: 3, employeeId: 8),
        User(9, "P1009", roleId: 3, employeeId: 9),
        User(10, "P1010", roleId: 3, employeeId: 10),
        User(11, "P1011", roleId: 3, employeeId: 11),
        User(12, "P1012", roleId: 3, employeeId: 12),
        User(13, "P1013", roleId: 3, employeeId: 13),
        User(14, "admin", roleId: 1, employeeId: null),
        User(15, "accounting", roleId: 2, employeeId: null),
    };

    private static object Emp(int id, string no, string name, int deptId, string hotel, int y, int m, int d, DateOnly? termination = null) =>
        new
        {
            Id = id,
            EmployeeNo = no,
            FullName = name,
            DepartmentId = deptId,
            Hotel = (string?)hotel,
            HireDate = new DateOnly(y, m, d),
            TerminationDate = termination,
        };

    private static object User(int id, string username, int roleId, int? employeeId) =>
        new
        {
            Id = id,
            Username = username,
            RoleId = roleId,
            EmployeeId = employeeId,
            PasswordHash = (string?)DefaultPasswordHash,
            MustChangePassword = true,
            IsActive = true,
            CreatedAt = SeededAtUtc,
        };
}
