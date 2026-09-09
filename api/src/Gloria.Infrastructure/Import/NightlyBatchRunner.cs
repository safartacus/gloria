using Gloria.Application.Commissions;
using Gloria.Application.Imports;
using Gloria.Application.Personnel;
using Gloria.Domain.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gloria.Infrastructure.Import;

/// <summary>
/// The end-of-day pipeline as one callable unit, shared by the scheduled
/// <see cref="NightlyImportHostedService"/> and the manual "run now" endpoint:
/// <list type="number">
///   <item>sync the personel.csv roster (additive)</item>
///   <item>import the PMS/POS/ERP CSVs from the inbox / data folder</item>
///   <item>recalculate commissions for every period the import touched, and read back its summary</item>
/// </list>
/// Every step is idempotent, so repeating a run is harmless.
/// </summary>
public sealed class NightlyBatchRunner
{
    private readonly PersonnelSyncService _personnelSync;
    private readonly PersonnelSyncSource _personnelSource;
    private readonly EndOfDayImportService _endOfDay;
    private readonly CommissionCalculationService _commissions;
    private readonly NightlyImportOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<NightlyBatchRunner> _logger;

    public NightlyBatchRunner(
        PersonnelSyncService personnelSync,
        PersonnelSyncSource personnelSource,
        EndOfDayImportService endOfDay,
        CommissionCalculationService commissions,
        IOptions<NightlyImportOptions> options,
        IHostEnvironment environment,
        ILogger<NightlyBatchRunner> logger)
    {
        _personnelSync = personnelSync;
        _personnelSource = personnelSource;
        _endOfDay = endOfDay;
        _commissions = commissions;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<NightlyBatchResult> RunAsync(CancellationToken ct = default)
    {
        // Tag every log line from this run (personnel + import + recalc) with one id so a nightly
        // run's quarantined rows can be pulled together in Seq.
        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["BatchRunId"] = Guid.NewGuid().ToString("N") });

        var personnel = await SyncPersonnelAsync(ct);
        var (import, importSkippedReason) = await ImportAsync(ct);
        var calculations = import is null
            ? Array.Empty<PeriodCalculationOutcome>()
            : await RecalculateAsync(import.AffectedPeriods, ct);

        return new NightlyBatchResult(DateTime.UtcNow, personnel, import, importSkippedReason, calculations);
    }

    private async Task<PersonnelSyncResult> SyncPersonnelAsync(CancellationToken ct)
    {
        if (!_personnelSource.Exists)
        {
            _logger.LogInformation("Personnel sync: {Path} not found; skipping.", _personnelSource.ResolvedPath);
            return new PersonnelSyncResult(0, 0, 0, Array.Empty<string>(),
                new[] { $"{_personnelSource.ResolvedPath} bulunamadı." }, Array.Empty<SyncedCredential>());
        }

        await using var stream = _personnelSource.Open();
        var result = await _personnelSync.SyncAsync(stream, ct);
        _logger.LogInformation(
            "Personnel sync: {Departments} departments, {Employees} employees, {Users} logins added; {Skipped} already present, {Errors} errors.",
            result.DepartmentsAdded, result.EmployeesAdded, result.UsersAdded, result.Skipped.Count, result.Errors.Count);
        return result;
    }

    private async Task<(EndOfDayImportResult? Result, string? SkippedReason)> ImportAsync(CancellationToken ct)
    {
        var inbox = ResolveInboxDirectory();
        if (inbox is null)
            return (null, "İçe aktarılacak klasör bulunamadı (Gloria:NightlyImport:InboxDirectory ayarlı değil, data/ klasörü de yok).");

        var files = new[]
            {
                ("PMS", NewestMatch(inbox, _options.PmsPattern)),
                ("POS", NewestMatch(inbox, _options.PosPattern)),
                ("ERP", NewestMatch(inbox, _options.ErpPattern)),
            }
            .Where(x => x.Item2 is not null)
            .Select(x => (Code: x.Item1, Path: x.Item2!))
            .ToList();

        if (files.Count == 0)
            return (null, $"'{inbox}' klasöründe {_options.PmsPattern} / {_options.PosPattern} / {_options.ErpPattern} deseniyle eşleşen dosya yok.");

        var streams = files.Select(f => (f.Code, f.Path, Stream: (Stream)File.OpenRead(f.Path))).ToList();
        EndOfDayImportResult result;
        try
        {
            var sources = streams
                .Select(s => new SourceFileInput(s.Code, Path.GetFileName(s.Path), s.Stream))
                .ToList<SourceFileInput>();

            var businessDay = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            result = await _endOfDay.RunAsync(businessDay, sources, ct);

            _logger.LogInformation(
                "Import batch #{Batch} ({Day}): read {Read}, inserted {Inserted}, duplicate {Duplicate}, failed {Failed}, reconciled {Reconciled}; periods {Periods}.",
                result.BatchId, result.BusinessDay, result.RowsRead, result.RowsInserted,
                result.RowsDuplicate, result.RowsFailed, result.RowsReconciled,
                string.Join(", ", result.AffectedPeriods.Select(p => $"{p.Year}-{p.Month:D2}")));
        }
        finally
        {
            foreach (var s in streams) s.Stream.Dispose();
        }

        Archive(files.Select(f => f.Path));
        return (result, null);
    }

    private async Task<IReadOnlyList<PeriodCalculationOutcome>> RecalculateAsync(
        IReadOnlyList<AffectedPeriod> periods, CancellationToken ct)
    {
        var outcomes = new List<PeriodCalculationOutcome>();
        foreach (var period in periods)
        {
            bool calculated;
            string? skippedReason = null;
            try
            {
                await _commissions.CalculateAsync(new CommissionCalculationRequest(period.Year, period.Month, null), ct);
                calculated = true;
            }
            catch (DomainException ex)
            {
                calculated = false;
                skippedReason = ex.Message; // e.g. period is closed
                _logger.LogWarning("Recalculation skipped for {Year}-{Month:D2}: {Reason}", period.Year, period.Month, ex.Message);
            }

            var summary = await _commissions.GetPeriodSummaryAsync(period.Year, period.Month, ct);
            outcomes.Add(new PeriodCalculationOutcome(
                period.Year, period.Month, calculated, skippedReason,
                summary.EmployeeCount, summary.TotalCommission, summary.TotalRefunds, summary.TotalUnmatched));
        }
        return outcomes;
    }

    private string? ResolveInboxDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_options.InboxDirectory) && Directory.Exists(_options.InboxDirectory))
            return _options.InboxDirectory;

        var dir = new DirectoryInfo(_environment.ContentRootPath);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }

    private static string? NewestMatch(string directory, string pattern)
    {
        var matches = Directory.GetFiles(directory, pattern);
        return matches.Length == 0 ? null : matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    private void Archive(IEnumerable<string> paths)
    {
        if (string.IsNullOrWhiteSpace(_options.ArchiveDirectory)) return;

        var target = Path.Combine(_options.ArchiveDirectory, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(target);
        foreach (var path in paths)
        {
            try
            {
                File.Move(path, Path.Combine(target, Path.GetFileName(path)), overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not archive {Path}", path);
            }
        }
    }
}
