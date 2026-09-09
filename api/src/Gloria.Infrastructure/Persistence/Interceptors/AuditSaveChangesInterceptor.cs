using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gloria.Application.Abstractions;
using Gloria.Domain.Auditing;
using Gloria.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gloria.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes an append-only <see cref="AuditEntry"/> for every insert/update/delete of an
/// <see cref="IAuditable"/> aggregate: entity, id, action, field-level old/new values, who, when.
/// Each entry carries the previous entry's hash (SHA-256 chain) so tampering is detectable.
/// DB-generated keys are resolved after the first save, then the audit rows are persisted.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly List<PendingAudit> _pending = new();

    public AuditSaveChangesInterceptor(ICurrentUser currentUser, IClock clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        _pending.Clear();
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var changesJson = BuildChangesJson(entry);

            // Skip spurious "modified but nothing actually changed" entries.
            if (entry.State == EntityState.Modified && changesJson == "{}")
                continue;

            _pending.Add(new PendingAudit(
                entry,
                entry.Metadata.ClrType.Name,
                MapAction(entry.State),
                changesJson));
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0 || eventData.Context is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        var username = _currentUser.IsAuthenticated ? _currentUser.Username : "system";
        var roleCode = _currentUser.IsAuthenticated ? _currentUser.RoleCode : "SYSTEM";
        var now = _clock.UtcNow;

        var previousHash = (await context.Set<AuditEntry>()
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken))?.Hash;

        var toInsert = _pending.ToList();
        _pending.Clear();

        foreach (var pending in toInsert)
        {
            var entityId = ReadKey(pending.Entry);
            var entry = new AuditEntry(pending.EntityName, entityId, pending.Action, pending.ChangesJson,
                username, roleCode, now);

            var hash = ComputeHash(previousHash, entry);
            entry.SetHashChain(previousHash, hash);
            previousHash = hash;

            context.Set<AuditEntry>().Add(entry);
        }

        await context.SaveChangesAsync(cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static string MapAction(EntityState state) => state switch
    {
        EntityState.Added => AuditActions.Insert,
        EntityState.Modified => AuditActions.Update,
        EntityState.Deleted => AuditActions.Delete,
        _ => state.ToString().ToUpperInvariant()
    };

    private static string ReadKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return "?";
        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");
        return string.Join("|", values);
    }

    private static string BuildChangesJson(EntityEntry entry)
    {
        var payload = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsPrimaryKey()) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    payload[prop.Metadata.Name] = new { @new = prop.CurrentValue };
                    break;
                case EntityState.Deleted:
                    payload[prop.Metadata.Name] = new { old = prop.OriginalValue };
                    break;
                case EntityState.Modified when prop.IsModified &&
                                               !Equals(prop.OriginalValue, prop.CurrentValue):
                    payload[prop.Metadata.Name] = new { old = prop.OriginalValue, @new = prop.CurrentValue };
                    break;
            }
        }
        return JsonSerializer.Serialize(payload);
    }

    private static string ComputeHash(string? previousHash, AuditEntry entry)
    {
        var canonical = $"{previousHash}|{entry.EntityName}|{entry.EntityId}|{entry.Action}|{entry.ChangesJson}|" +
                        $"{entry.Username}|{entry.RoleCode}|{entry.TimestampUtc:O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private sealed record PendingAudit(EntityEntry Entry, string EntityName, string Action, string ChangesJson);
}
