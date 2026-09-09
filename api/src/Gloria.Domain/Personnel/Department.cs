using Gloria.Domain.Common;

namespace Gloria.Domain.Personnel;

/// <summary>
/// The department an employee belongs to (SPA, F&amp;B, RECREATION ...). Rows are derived from the
/// distinct "Department" values in personel.csv during import.
/// </summary>
public sealed class Department : AggregateRoot<int>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private Department() { }

    public static Department Create(string code, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Department code is required.");
        var trimmed = code.Trim();
        return new Department
        {
            Code = trimmed.ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(name) ? trimmed : name.Trim(),
            IsActive = true
        };
    }
}
