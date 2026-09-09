namespace Gloria.Domain.Personnel;

public interface IDepartmentRepository
{
    Task<Department?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Department department, CancellationToken ct = default);
}

public interface IEmployeeRepository
{
    Task<Employee?> GetByEmployeeNoAsync(string employeeNo, CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
}
