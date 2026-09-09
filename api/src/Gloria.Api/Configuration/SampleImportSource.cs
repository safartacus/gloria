using Gloria.Application.Imports;
using Microsoft.Extensions.Options;

namespace Gloria.Api.Configuration;

public sealed class SampleImportOptions
{
    public const string SectionName = "Gloria:SampleImport";

    /// <summary>Folder holding the sample sales CSVs. Empty = auto-detect the repo's /data folder.</summary>
    public string Directory { get; set; } = string.Empty;
    public string BusinessDay { get; set; } = "2026-08-31";
    public string Pms { get; set; } = "pms_fidelio_2026_08.csv";
    public string Pos { get; set; } = "pos_flyby_2026_08.csv";
    public string Erp { get; set; } = "erp_jde_2026_08.csv";
}

/// <summary>
/// Locates the bundled sample sales CSVs for the "try with sample data" button. These files are
/// read only when that endpoint is called — nothing is loaded at startup.
/// </summary>
public sealed class SampleImportSource
{
    private readonly SampleImportOptions _options;
    private readonly string _directory;

    public SampleImportSource(IOptions<SampleImportOptions> options, IHostEnvironment env)
    {
        _options = options.Value;
        _directory = ResolveDirectory(_options.Directory, env.ContentRootPath);
    }

    public string BusinessDay => _options.BusinessDay;

    /// <summary>Opens read streams for whichever of PMS/POS/ERP sample files exist. The caller disposes them.</summary>
    public IReadOnlyList<SourceFileInput> Open()
    {
        var wanted = new[] { ("PMS", _options.Pms), ("POS", _options.Pos), ("ERP", _options.Erp) };
        var result = new List<SourceFileInput>();

        foreach (var (code, fileName) in wanted)
        {
            var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(_directory, fileName);
            if (File.Exists(path))
                result.Add(new SourceFileInput(code, Path.GetFileName(path), File.OpenRead(path)));
        }

        return result;
    }

    private static string ResolveDirectory(string configured, string contentRoot)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        var dir = new DirectoryInfo(contentRoot);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate)) return candidate;
        }
        return Path.Combine(contentRoot, "data");
    }
}
