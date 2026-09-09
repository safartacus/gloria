namespace Gloria.Application.Abstractions;

public sealed record CredentialFileEntry(
    string Username,
    string? EmployeeNo,
    string RoleCode,
    string FullName,
    string Password);

/// <summary>
/// Writes a one-per-user plain-text credential note when a new account is created, so the admin can
/// hand it to the person. The note tells the user to change the password after first login.
/// </summary>
public interface ICredentialFileWriter
{
    /// <returns>The absolute path of the file written, or null if writing is disabled/unavailable.</returns>
    string? Write(CredentialFileEntry entry);
}
