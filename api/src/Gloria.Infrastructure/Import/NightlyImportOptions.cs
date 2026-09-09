namespace Gloria.Infrastructure.Import;

/// <summary>
/// Configuration for the unattended nightly end-of-day import. Disabled by default; when enabled,
/// a hosted service watches <see cref="InboxDirectory"/> and runs the same
/// <c>EndOfDayImportService</c> the API endpoint uses.
/// </summary>
public sealed class NightlyImportOptions
{
    public const string SectionName = "Gloria:NightlyImport";

    public bool Enabled { get; set; }

    /// <summary>Local time of day to run, "HH:mm".</summary>
    public string RunAtLocalTime { get; set; } = "02:00";

    /// <summary>Folder the nightly PMS/POS/ERP export files are dropped into (SFTP / share / connector output).</summary>
    public string InboxDirectory { get; set; } = string.Empty;

    /// <summary>Processed files are moved here (timestamped subfolder). Empty = leave them in place.</summary>
    public string ArchiveDirectory { get; set; } = string.Empty;

    public string PmsPattern { get; set; } = "pms_*.csv";
    public string PosPattern { get; set; } = "pos_*.csv";
    public string ErpPattern { get; set; } = "erp_*.csv";
}
