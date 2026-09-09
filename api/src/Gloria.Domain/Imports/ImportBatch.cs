using Gloria.Domain.Common;

namespace Gloria.Domain.Imports;

public static class ImportStatuses
{
    public const string Started = "STARTED";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
}

/// <summary>
/// A single "end-of-day" import run. All source files are processed under this batch; idempotent
/// (natural key + unique index) — re-running the same batch does not insert duplicates.
/// </summary>
public sealed class ImportBatch : AggregateRoot<int>
{
    private readonly List<ImportError> _errors = new();

    public string BusinessDay { get; private set; } = default!;   // e.g. "2026-08-31"
    public string Description { get; private set; } = default!;
    public string Status { get; private set; } = ImportStatuses.Started;
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public int RowsRead { get; private set; }
    public int RowsInserted { get; private set; }
    public int RowsDuplicate { get; private set; }
    public int RowsFailed { get; private set; }
    public int RowsReconciled { get; private set; }              // ERP -> PMS matched & skipped (double-count avoided)
    public string RunByUsername { get; private set; } = default!;

    public IReadOnlyList<ImportError> Errors => _errors;

    private ImportBatch() { }

    public static ImportBatch Start(string businessDay, string description, string runByUsername, DateTime now) => new()
    {
        BusinessDay = businessDay,
        Description = description,
        Status = ImportStatuses.Started,
        StartedAt = now,
        RunByUsername = runByUsername
    };

    public void RowRead() => RowsRead++;
    public void RowInserted() => RowsInserted++;
    public void RowSkippedDuplicate() => RowsDuplicate++;
    public void RowReconciled() => RowsReconciled++;

    public void AddError(string sourceSystemCode, int rowNumber, string rawContent, string errorCode, string errorMessage, DateTime now)
    {
        RowsFailed++;
        _errors.Add(new ImportError(sourceSystemCode, rowNumber, rawContent, errorCode, errorMessage, now));
    }

    public void Complete(DateTime now)
    {
        Status = ImportStatuses.Completed;
        FinishedAt = now;
    }

    public void Fail(DateTime now)
    {
        Status = ImportStatuses.Failed;
        FinishedAt = now;
    }
}
