using Gloria.Domain.Common;

namespace Gloria.Domain.Reference;

/// <summary>
/// The source system a sale originates from (PMS / POS / ERP). Lookup table; rows are loaded
/// from data/reference/source-system.csv.
/// </summary>
public sealed class SourceSystem : Entity<int>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    private SourceSystem() { }

    public static SourceSystem Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Source system code is required.");
        return new SourceSystem { Code = code.Trim().ToUpperInvariant(), Name = name.Trim() };
    }

    public void Update(string name) => Name = name.Trim();
}

public static class SourceSystemCodes
{
    public const string Pms = "PMS";
    public const string Pos = "POS";
    public const string Erp = "ERP";
}
