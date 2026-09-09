using Gloria.Application.Abstractions;
using Gloria.Application.Auth;
using Gloria.Domain.Common;
using Gloria.Domain.Personnel;
using Gloria.Domain.Users;

namespace Gloria.Application.Users;

/// <summary>
/// Admin user management: list accounts, create a new account (random password, credential note),
/// deactivate / reactivate.
/// </summary>
public sealed class UserAdminService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IEmployeeRepository _employees;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICredentialFileWriter _credentialFiles;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public UserAdminService(
        IUserRepository users, IRoleRepository roles, IEmployeeRepository employees, IPasswordHasher passwordHasher,
        ICredentialFileWriter credentialFiles, IUnitOfWork uow, IClock clock)
    {
        _users = users;
        _roles = roles;
        _employees = employees;
        _passwordHasher = passwordHasher;
        _credentialFiles = credentialFiles;
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await _users.GetAllAsync(ct);
        var employeesById = (await _employees.GetAllAsync(ct)).ToDictionary(e => e.Id);

        return users
            .Select(u =>
            {
                var employee = u.EmployeeId is int id ? employeesById.GetValueOrDefault(id) : null;
                return new UserDto(
                    u.Username,
                    employee?.FullName ?? u.Username,
                    u.Role.Code,
                    employee?.EmployeeNo,
                    u.IsActive,
                    u.MustChangePassword,
                    u.CreatedAt);
            })
            .OrderBy(u => u.RoleCode).ThenBy(u => u.Username)
            .ToList();
    }

    public async Task<CreatedUserResult> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Kullanıcı adı zorunlu.");
        if (await _users.ExistsAsync(username, ct))
            throw new DomainException($"'{username}' kullanıcı adı zaten var.");

        var role = await _roles.GetByCodeAsync(request.RoleCode, ct)
            ?? throw new DomainException($"'{request.RoleCode}' rolü bulunamadı.");

        Employee? employee = null;
        if (role.Code == RoleCodes.Employee)
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeNo))
                throw new DomainException("PERSONEL rolü için personel no zorunlu.");
            employee = await _employees.GetByEmployeeNoAsync(request.EmployeeNo.Trim(), ct)
                ?? throw new DomainException($"'{request.EmployeeNo}' personeli bulunamadı.");
        }

        var user = User.Create(username, role, employee?.Id, _clock.UtcNow);
        var password = PasswordGenerator.Generate();
        user.SetInitialPasswordHash(_passwordHasher.Hash(password));
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var fullName = employee?.FullName ?? username;
        var filePath = _credentialFiles.Write(
            new CredentialFileEntry(username, employee?.EmployeeNo, role.Code, fullName, password));

        return new CreatedUserResult(username, fullName, role.Code, password, filePath);
    }

    public async Task SetActiveAsync(string username, bool active, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct)
            ?? throw new DomainException($"'{username}' kullanıcısı bulunamadı.");
        if (active) user.Activate(); else user.Deactivate();
        await _uow.SaveChangesAsync(ct);
    }
}
