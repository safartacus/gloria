namespace Gloria.Domain.Common;

/// <summary>
/// Base class for entities compared by identity. Two entities are equal when they are of the
/// same type and share the same non-default Id.
/// </summary>
public abstract class Entity<TId>
    where TId : struct, IEquatable<TId>
{
    public TId Id { get; protected set; }

    public bool IsTransient => Id.Equals(default);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (IsTransient || other.IsTransient) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => (GetType(), Id).GetHashCode();

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) => Equals(a, b);
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !Equals(a, b);
}
