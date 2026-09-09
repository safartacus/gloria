using Gloria.Domain.Personnel;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly GloriaDbContext _db;
    public DepartmentRepository(GloriaDbContext db) => _db = db;

    public Task<Department?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Departments.FirstOrDefaultAsync(d => d.Code == code.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Departments.OrderBy(d => d.Code).ToListAsync(ct);

    public async Task AddAsync(Department department, CancellationToken ct = default) =>
        await _db.Departments.AddAsync(department, ct);
}

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly GloriaDbContext _db;
    public EmployeeRepository(GloriaDbContext db) => _db = db;

    public Task<Employee?> GetByEmployeeNoAsync(string employeeNo, CancellationToken ct = default) =>
        _db.Employees.FirstOrDefaultAsync(e => e.EmployeeNo == employeeNo, ct);

    public Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Employees.OrderBy(e => e.EmployeeNo).ToListAsync(ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
        await _db.Employees.AddAsync(employee, ct);
}
