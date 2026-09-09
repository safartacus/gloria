namespace Gloria.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
