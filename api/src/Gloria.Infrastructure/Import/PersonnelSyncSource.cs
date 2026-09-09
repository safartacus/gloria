using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Gloria.Infrastructure.Import;

public sealed class PersonnelSyncOptions
{
    public const string SectionName = "Gloria:PersonnelSync";

    /// <summary>
    /// Explicit path to personel.csv. Empty = auto-detect a <c>data/personel.csv</c> by walking up
    /// from the content root (works both in the repo and in the container where <c>./data</c> is mounted).
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Locates the <c>personel.csv</c> roster file used by the additive personnel sync (nightly job and
/// the "senkronize et" button). Opening is caller's responsibility; the stream is not owned here.
/// </summary>
public sealed class PersonnelSyncSource
{
    private readonly string _path;

    public PersonnelSyncSource(IOptions<PersonnelSyncOptions> options, IHostEnvironment environment)
        => _path = Resolve(options.Value.FilePath, environment.ContentRootPath);

    public string ResolvedPath => _path;

    public bool Exists => File.Exists(_path);

    public Stream Open() => File.OpenRead(_path);

    private static string Resolve(string configured, string contentRoot)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        var dir = new DirectoryInfo(contentRoot);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "data", "personel.csv");
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(contentRoot, "data", "personel.csv");
    }
}
