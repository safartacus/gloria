namespace Gloria.Application.Abstractions;

/// <summary>
/// The identity of the caller for the current request. Populated by the API layer from the
/// authenticated principal (whose role is resolved from the Users table, not the raw header).
/// Consumed by the audit interceptor and by row-level authorization.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string Username { get; }
    string FullName { get; }
    string RoleCode { get; }
    int? EmployeeId { get; }
}
