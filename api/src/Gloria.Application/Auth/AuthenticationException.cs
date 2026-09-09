namespace Gloria.Application.Auth;

/// <summary>Thrown for any login / token failure. Mapped to HTTP 401 at the API layer.</summary>
public sealed class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
