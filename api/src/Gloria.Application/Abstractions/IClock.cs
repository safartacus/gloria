namespace Gloria.Application.Abstractions;

/// <summary>Abstracts the system clock so time-dependent logic stays testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
