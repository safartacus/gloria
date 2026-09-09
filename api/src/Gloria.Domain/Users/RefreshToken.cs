using Gloria.Domain.Common;

namespace Gloria.Domain.Users;

/// <summary>
/// A server-side refresh token record. Only the SHA-256 hash of the token is stored. Rotation:
/// on use, the token is revoked and points to its replacement, so token reuse is detectable.
/// </summary>
public sealed class RefreshToken : AggregateRoot<long>
{
    public int UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(int userId, string tokenHash, DateTime nowUtc, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("Token hash is required.");
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(lifetime),
        };
    }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void Revoke(DateTime nowUtc, string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null) return;
        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
