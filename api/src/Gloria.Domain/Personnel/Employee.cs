using Gloria.Domain.Common;

namespace Gloria.Domain.Personnel;

/// <summary>An employee eligible for commission. Loaded from personel.csv.</summary>
public sealed class Employee : AggregateRoot<int>
{
    public string EmployeeNo { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public int DepartmentId { get; private set; }
    public string? Hotel { get; private set; }
    public DateOnly HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }

    private Employee() { }

    public static Employee Create(string employeeNo, string fullName, int departmentId, string? hotel,
        DateOnly hireDate, DateOnly? terminationDate)
    {
        if (string.IsNullOrWhiteSpace(employeeNo)) throw new DomainException("Employee number is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required.");
        if (terminationDate is not null && terminationDate < hireDate)
            throw new DomainException("Termination date cannot precede the hire date.");

        return new Employee
        {
            EmployeeNo = employeeNo.Trim(),
            FullName = fullName.Trim(),
            DepartmentId = departmentId,
            Hotel = hotel?.Trim(),
            HireDate = hireDate,
            TerminationDate = terminationDate
        };
    }

    public bool IsActiveOn(DateOnly date) =>
        date >= HireDate && (TerminationDate is null || date <= TerminationDate);

    public void UpdateDetails(string fullName, int departmentId, string? hotel, DateOnly? terminationDate)
    {
        FullName = fullName.Trim();
        DepartmentId = departmentId;
        Hotel = hotel?.Trim();
        TerminationDate = terminationDate;
    }
}
