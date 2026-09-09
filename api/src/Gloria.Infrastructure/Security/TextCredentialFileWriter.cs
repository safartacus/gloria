using System.Text;
using Gloria.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gloria.Infrastructure.Security;

public sealed class CredentialFileOptions
{
    public const string SectionName = "Gloria:Credentials";

    /// <summary>Directory the per-user notes are written to. Empty disables file writing.</summary>
    public string Directory { get; set; } = string.Empty;
}

/// <summary>
/// Writes <c>&lt;username&gt;_&lt;employeeNo|username&gt;.txt</c> containing role, name, username, password
/// and a "change your password after logging in" notice.
/// </summary>
public sealed class TextCredentialFileWriter : ICredentialFileWriter
{
    private readonly string _directory;
    private readonly ILogger<TextCredentialFileWriter> _logger;

    public TextCredentialFileWriter(IOptions<CredentialFileOptions> options, ILogger<TextCredentialFileWriter> logger)
    {
        _directory = options.Value.Directory?.Trim() ?? string.Empty;
        _logger = logger;
    }

    public string? Write(CredentialFileEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_directory)) return null;

        try
        {
            System.IO.Directory.CreateDirectory(_directory);

            var codePart = Sanitize(string.IsNullOrWhiteSpace(entry.EmployeeNo) ? entry.Username : entry.EmployeeNo!);
            var path = Path.Combine(_directory, $"{Sanitize(entry.Username)}_{codePart}.txt");

            var content = new StringBuilder()
                .AppendLine($"Rol: {entry.RoleCode}")
                .AppendLine($"İsim: {entry.FullName}")
                .AppendLine($"Kullanıcı adı: {entry.Username}")
                .AppendLine($"Şifre: {entry.Password}")
                .AppendLine()
                .AppendLine("Lütfen giriş yaptıktan sonra şifrenizi değiştirin.")
                .ToString();

            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write credential file for {Username}", entry.Username);
            return null;
        }
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
}
