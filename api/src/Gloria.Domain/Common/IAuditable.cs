namespace Gloria.Domain.Common;

/// <summary>
/// Every Insert/Update/Delete on an aggregate implementing this interface is written to the
/// append-only audit log (who / when / old value / new value) by the infrastructure-layer
/// SaveChanges interceptor.
/// </summary>
public interface IAuditable
{
}
