using System.Globalization;
using Gloria.Application.Abstractions;
using Gloria.Application.Auth;
using Gloria.Domain.Personnel;
using Gloria.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Gloria.Application.Personnel;

public sealed record SyncedCredential(string Username, string Password);

public sealed record PersonnelSyncResult(
    int DepartmentsAdded,
    int EmployeesAdded,
    int UsersAdded,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Errors,
    IReadOnlyList<SyncedCredential> Credentials);

/// <summary>
/// Additive personnel sync: reads a personel.csv-shaped source and inserts any employee (and its
/// EMPLOYEE-role login) that is not already in the database. Existing rows are left untouched.
/// Idempotent — safe to run nightly and on demand.
/// </summary>
public sealed class PersonnelSyncService
{
    private readonly ICsvFileReader _csv;
    private readonly IDepartmentRepository _departments;
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICredentialFileWriter _credentialFiles;
    private readonly ICacheStore _cache;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ILogger<PersonnelSyncService> _logger;

    public PersonnelSyncService(
        ICsvFileReader csv, IDepartmentRepository departments, IEmployeeRepository employees, IUserRepository users,
        IRoleRepository roles, IPasswordHasher passwordHasher, ICredentialFileWriter credentialFiles,
        ICacheStore cache, IUnitOfWork uow, IClock clock, ILogger<PersonnelSyncService> logger)
    {
        _csv = csv;
        _departments = departments;
        _employees = employees;
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _credentialFiles = credentialFiles;
        _cache = cache;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PersonnelSyncResult> SyncAsync(Stream personnelCsv, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var rows = _csv.Read(personnelCsv);
        var skipped = new List<string>();
        var errors = new List<string>();

        // 1) missing departments
        var deptsAdded = 0;
        foreach (var code in rows
                     .Select(r => r.Get("Departman"))
                     .Where(c => !string.IsNullOrWhiteSpace(c))
                     .Select(c => c!.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await _departments.GetByCodeAsync(code, ct) is not null) continue;
            await _departments.AddAsync(Department.Create(code), ct);
            deptsAdded++;
        }
        if (deptsAdded > 0) await _uow.SaveChangesAsync(ct);

        // 2) missing employees
        var newEmployees = new List<(Employee Employee, string FullName)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            try
            {
                var employeeNo = row.Required("PersonelNo");
                if (!seen.Add(employeeNo))
                    continue; // duplicate line for the same person in this file

                if (await _employees.GetByEmployeeNoAsync(employeeNo, ct) is not null)
                {
                    skipped.Add($"{employeeNo}: zaten kayıtlı");
                    continue;
                }

                var dept = await _departments.GetByCodeAsync(row.Required("Departman"), ct)
                    ?? throw new InvalidOperationException($"Departman '{row.Get("Departman")}' bulunamadı.");

                var employee = Employee.Create(
                    employeeNo, row.Required("AdSoyad"), dept.Id, row.Get("Otel"),
                    ParseDate(row.Required("IseBaslamaTarihi")), ParseNullableDate(row.Get("AyrilisTarihi")));

                await _employees.AddAsync(employee, ct);
                newEmployees.Add((employee, employee.FullName));
            }
            catch (Exception ex)
            {
                errors.Add($"satır {row.LineNumber}: {ex.Message}");
                _logger.LogWarning(ex,
                    "Personnel sync: row {PersonnelLine} rejected ({PersonnelNo}) — {Reason}. Raw: {PersonnelRawRow}",
                    row.LineNumber, row.Get("PersonelNo") ?? "?", ex.Message, row.Raw);
            }
        }
        if (newEmployees.Count > 0) await _uow.SaveChangesAsync(ct); // materializes employee ids

        // 3) EMPLOYEE-role logins for the new employees
        var credentials = new List<SyncedCredential>();
        var employeeRole = await _roles.GetByCodeAsync(RoleCodes.Employee, ct);
        if (employeeRole is null)
        {
            errors.Add($"'{RoleCodes.Employee}' rolü yok — hesap oluşturulamadı.");
            _logger.LogWarning("Personnel sync: '{Role}' role missing — {Count} new employee(s) left without a login.",
                RoleCodes.Employee, newEmployees.Count);
        }
        else
        {
            foreach (var (employee, fullName) in newEmployees)
            {
                if (await _users.ExistsAsync(employee.EmployeeNo, ct)) continue;

                var user = User.Create(employee.EmployeeNo, employeeRole, employee.Id, now);
                var password = PasswordGenerator.Generate();
                user.SetInitialPasswordHash(_passwordHasher.Hash(password));
                await _users.AddAsync(user, ct);
                credentials.Add(new SyncedCredential(employee.EmployeeNo, password));
                _credentialFiles.Write(new CredentialFileEntry(
                    employee.EmployeeNo, employee.EmployeeNo, employeeRole.Code, fullName, password));
            }
            if (credentials.Count > 0) await _uow.SaveChangesAsync(ct);
        }

        if (deptsAdded > 0 || newEmployees.Count > 0)
        {
            await _cache.RemoveAsync(CacheKeys.Employees, ct);
            await _cache.RemoveAsync(CacheKeys.Departments, ct);
        }

        if (errors.Count > 0)
            _logger.LogWarning("Personnel sync finished with {ErrorCount} rejected row(s) of {RowCount} read.",
                errors.Count, rows.Count);

        return new PersonnelSyncResult(deptsAdded, newEmployees.Count, credentials.Count, skipped, errors, credentials);
    }

    private static DateOnly ParseDate(string raw) =>
        DateOnly.ParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly? ParseNullableDate(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : ParseDate(raw);
}
