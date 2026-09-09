using Gloria.Application.Abstractions;

namespace Gloria.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
