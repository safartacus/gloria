using Gloria.Domain.Common;

namespace Gloria.Domain.Auditing;

public static class AuditActions
{
    public const string Insert = "INSERT";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
}

/// <summary>
/// An immutable (append-only) audit entry. Produced by the infrastructure SaveChanges interceptor
/// for every change to an IAuditable aggregate. Tamper-evident via an optional hash chain.
/// </summary>
public sealed class AuditEntry : Entity<long>
{
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string ChangesJson { get; private set; } = default!;
    public string Username { get; private set; } = default!;
    public string RoleCode { get; private set; } = default!;
    public DateTime TimestampUtc { get; private set; }
    public string? PreviousHash { get; private set; }
    public string? Hash { get; private set; }

    private AuditEntry() { }

    public AuditEntry(string entityName, string entityId, string action, string changesJson,
        string username, string roleCode, DateTime timestampUtc)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        ChangesJson = changesJson;
        Username = username;
        RoleCode = roleCode;
        TimestampUtc = timestampUtc;
    }

    public void SetHashChain(string? previousHash, string hash)
    {
        PreviousHash = previousHash;
        Hash = hash;
    }
}
