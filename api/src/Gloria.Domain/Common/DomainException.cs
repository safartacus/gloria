namespace Gloria.Domain.Common;

/// <summary>
/// Signals that a business rule / invariant was violated. Translated to HTTP 409/422 at the API layer.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
