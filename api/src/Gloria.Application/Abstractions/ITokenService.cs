namespace Gloria.Application.Abstractions;

/// <summary>The identity claims embedded into an access token.</summary>
public sealed record TokenSubject(int UserId, string Username, string FullName, string RoleCode, int? EmployeeId);

public interface ITokenService
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }

    string CreateAccessToken(TokenSubject subject);

    /// <summary>Creates a new opaque refresh token: the raw value (returned to the client once) and its stored hash.</summary>
    (string Raw, string Hash) CreateRefreshToken();

    string HashRefreshToken(string raw);
}
