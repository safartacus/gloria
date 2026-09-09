using System.Reflection;
using System.Text;
using FluentAssertions;
using Gloria.Application.Abstractions;
using Gloria.Application.Personnel;
using Gloria.Domain.Common;
using Gloria.Domain.Personnel;
using Gloria.Domain.Users;
using Gloria.Infrastructure.Csv;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gloria.Application.Tests;

public class PersonnelSyncServiceTests
{
    private const string Csv =
        "PersonelNo;AdSoyad;Departman;Otel;IseBaslamaTarihi;AyrilisTarihi\n" +
        "P1001;Ayşe Demir;SPA;GSR;2024-03-01;\n" +          // already in the DB
        "P1014;Yeni Personel;SPA;GSR;2026-09-01;\n" +        // new, existing dept
        "P1015;Deniz Yıldız;RECREATION;GT;2026-09-02;\n" +   // new, brand-new dept
        "P1015;Deniz Yıldız;RECREATION;GT;2026-09-02;\n";    // duplicate line, ignored

    [Fact]
    public async Task SyncAsync_adds_only_missing_employees_departments_and_logins()
    {
        var h = new Harness(seededEmployeeNos: "P1001");

        var result = await h.Service.SyncAsync(Stream(Csv));

        result.DepartmentsAdded.Should().Be(1);              // RECREATION
        result.EmployeesAdded.Should().Be(2);                // P1014, P1015
        result.UsersAdded.Should().Be(2);
        result.Skipped.Should().ContainSingle().Which.Should().Contain("P1001");

        h.Employees.Items.Select(e => e.EmployeeNo).Should().BeEquivalentTo("P1001", "P1014", "P1015");
        h.Users.Items.Select(u => u.Username).Should().BeEquivalentTo("P1014", "P1015");
        h.Users.Items.Should().OnlyContain(u => u.MustChangePassword && u.Role.Code == RoleCodes.Employee);
        h.CredentialFiles.Should().HaveCount(2);
        result.Credentials.Select(c => c.Username).Should().BeEquivalentTo("P1014", "P1015");
        h.Cache.Removed.Should().Contain(new[] { CacheKeys.Employees, CacheKeys.Departments });
    }

    [Fact]
    public async Task SyncAsync_is_idempotent_second_run_is_a_no_op()
    {
        var h = new Harness(seededEmployeeNos: "P1001");

        await h.Service.SyncAsync(Stream(Csv));
        var second = await h.Service.SyncAsync(Stream(Csv));

        second.DepartmentsAdded.Should().Be(0);
        second.EmployeesAdded.Should().Be(0);
        second.UsersAdded.Should().Be(0);
        h.Employees.Items.Should().HaveCount(3);
        h.Users.Items.Should().HaveCount(2);
        h.CredentialFiles.Should().HaveCount(2);
    }

    private static MemoryStream Stream(string csv) => new(Encoding.UTF8.GetBytes(csv));

    private sealed class Harness
    {
        public readonly FakeDepartmentRepo Departments = new();
        public readonly FakeEmployeeRepo Employees = new();
        public readonly FakeUserRepo Users = new();
        public readonly FakeCache Cache = new();
        public readonly List<CredentialFileEntry> CredentialFiles = new();
        public readonly PersonnelSyncService Service;

        public Harness(params string[] seededEmployeeNos)
        {
            Departments.Seed(Department.Create("SPA"));
            Departments.Seed(Department.Create("F&B"));

            var employeeRole = Role.Create(RoleCodes.Employee, "Çalışan", null);
            var roles = new FakeRoleRepo(employeeRole);

            foreach (var no in seededEmployeeNos)
                Employees.Seed(Employee.Create(no, $"Seeded {no}", 1, "GSR", new DateOnly(2024, 1, 1), null));

            var credentialWriter = new DelegateCredentialWriter(e => { CredentialFiles.Add(e); return $"/tmp/{e.Username}.txt"; });

            Service = new PersonnelSyncService(
                new SemicolonCsvFileReader(), Departments, Employees, Users, roles,
                new FakePasswordHasher(), credentialWriter, Cache, new FakeUnitOfWork(Employees, Users, Departments),
                new FixedClock(new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc)),
                NullLogger<PersonnelSyncService>.Instance);
        }
    }

    private static void SetId<T>(T entity, int id) where T : Entity<int> =>
        typeof(Entity<int>).GetProperty(nameof(Entity<int>.Id))!
            .GetSetMethod(nonPublic: true)!.Invoke(entity, new object[] { id });

    private sealed class FakeDepartmentRepo : IDepartmentRepository
    {
        public readonly List<Department> Items = new();
        private int _nextId = 1;
        public void Seed(Department d) { SetId(d, _nextId++); Items.Add(d); }
        public Task<Department?> GetByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(d => string.Equals(d.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Department>>(Items);
        public Task AddAsync(Department department, CancellationToken ct = default) { Items.Add(department); return Task.CompletedTask; }
        public void Materialize() { foreach (var d in Items.Where(x => x.IsTransient)) SetId(d, _nextId++); }
    }

    private sealed class FakeEmployeeRepo : IEmployeeRepository
    {
        public readonly List<Employee> Items = new();
        private int _nextId = 1;
        public void Seed(Employee e) { SetId(e, _nextId++); Items.Add(e); }
        public Task<Employee?> GetByEmployeeNoAsync(string employeeNo, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(e => string.Equals(e.EmployeeNo, employeeNo.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(e => e.Id == id));
        public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Employee>>(Items);
        public Task AddAsync(Employee employee, CancellationToken ct = default) { Items.Add(employee); return Task.CompletedTask; }
        public void Materialize() { foreach (var e in Items.Where(x => x.IsTransient)) SetId(e, _nextId++); }
    }

    private sealed class FakeUserRepo : IUserRepository
    {
        public readonly List<User> Items = new();
        private int _nextId = 1;
        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
        public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Id == id));
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<User>>(Items);
        public Task<bool> ExistsAsync(string username, CancellationToken ct = default) =>
            Task.FromResult(Items.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
        public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(Items.Count);
        public Task AddAsync(User user, CancellationToken ct = default) { Items.Add(user); return Task.CompletedTask; }
        public void Materialize() { foreach (var u in Items.Where(x => x.IsTransient)) SetId(u, _nextId++); }
    }

    private sealed class FakeRoleRepo : IRoleRepository
    {
        private readonly List<Role> _roles;
        public FakeRoleRepo(params Role[] roles) { _roles = roles.ToList(); }
        public Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(_roles.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Role>>(_roles);
        public Task AddAsync(Role role, CancellationToken ct = default) { _roles.Add(role); return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly FakeEmployeeRepo _employees;
        private readonly FakeUserRepo _users;
        private readonly FakeDepartmentRepo _departments;
        public FakeUnitOfWork(FakeEmployeeRepo employees, FakeUserRepo users, FakeDepartmentRepo departments)
        { _employees = employees; _users = users; _departments = departments; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            _departments.Materialize();
            _employees.Materialize();
            _users.Materialize();
            return Task.FromResult(0);
        }
    }

    private sealed class FakeCache : ICacheStore
    {
        public readonly List<string> Removed = new();
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken ct = default) { Removed.Add(key); return Task.CompletedTask; }
        public Task<long> GetGenerationAsync(string generationKey, CancellationToken ct = default) => Task.FromResult(0L);
        public Task BumpGenerationAsync(string generationKey, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hash) => hash == "hash:" + password;
    }

    private sealed class DelegateCredentialWriter : ICredentialFileWriter
    {
        private readonly Func<CredentialFileEntry, string?> _write;
        public DelegateCredentialWriter(Func<CredentialFileEntry, string?> write) => _write = write;
        public string? Write(CredentialFileEntry entry) => _write(entry);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
