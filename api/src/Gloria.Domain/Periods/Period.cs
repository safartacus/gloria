using Gloria.Domain.Common;

namespace Gloria.Domain.Periods;

public static class PeriodStatuses
{
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";
}

/// <summary>
/// A commission calculation period (year + month). Once closed, the sale / rule / calculation
/// records belonging to it cannot be changed (enforced by the application layer and a SaveChanges guard).
/// </summary>
public sealed class Period : AggregateRoot<int>, IAuditable
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Status { get; private set; } = PeriodStatuses.Open;
    public string? ClosedByUsername { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsOpen => Status == PeriodStatuses.Open;

    private Period() { }

    public static Period Open(int year, int month, DateTime now)
    {
        if (year is < 2000 or > 2100) throw new DomainException("Year must be within 2000-2100.");
        if (month is < 1 or > 12) throw new DomainException("Month must be within 1-12.");
        return new Period { Year = year, Month = month, Status = PeriodStatuses.Open, CreatedAt = now };
    }

    public void Close(string username, DateTime now)
    {
        if (!IsOpen)
            throw new DomainException($"Period {Year}-{Month:D2} is already closed; it cannot be closed again.");
        Status = PeriodStatuses.Closed;
        ClosedByUsername = username;
        ClosedAt = now;
    }

    public void EnsureOpenForModification()
    {
        if (!IsOpen)
            throw new DomainException($"Period {Year}-{Month:D2} is closed; records for this period cannot be changed.");
    }
}
