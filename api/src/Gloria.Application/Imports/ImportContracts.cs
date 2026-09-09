using Gloria.Application.Personnel;

namespace Gloria.Application.Imports;

public sealed record ImportErrorDto(string SourceSystemCode, int RowNumber, string ErrorCode, string ErrorMessage, string RawContent);

/// <summary>A calendar month that the imported sales rows fell into (candidate for recalculation).</summary>
public sealed record AffectedPeriod(int Year, int Month);

/// <summary>One import batch (nightly or manual) with its counters and quarantined rows — for the admin day view.</summary>
public sealed record ImportBatchLogDto(
    int BatchId,
    string BusinessDay,
    string Description,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string RunByUsername,
    int RowsRead,
    int RowsInserted,
    int RowsDuplicate,
    int RowsFailed,
    int RowsReconciled,
    IReadOnlyList<ImportErrorDto> Errors);

public sealed record EndOfDayImportResult(
    int BatchId,
    string BusinessDay,
    string Status,
    int RowsRead,
    int RowsInserted,
    int RowsDuplicate,
    int RowsFailed,
    int RowsReconciled,
    IReadOnlyList<ImportErrorDto> Errors,
    IReadOnlyList<AffectedPeriod> AffectedPeriods);

/// <summary>
/// Result of the full nightly batch run: roster sync, folder import, then commission recalculation
/// and a fresh summary for every period the import touched. Returned by the manual "run now" endpoint
/// and logged by the scheduled job.
/// </summary>
public sealed record NightlyBatchResult(
    DateTime RanAtUtc,
    PersonnelSyncResult PersonnelSync,
    EndOfDayImportResult? Import,
    string? ImportSkippedReason,
    IReadOnlyList<PeriodCalculationOutcome> Calculations);

public sealed record PeriodCalculationOutcome(
    int Year,
    int Month,
    bool Calculated,
    string? SkippedReason,
    int EmployeeCount,
    decimal TotalCommission,
    int TotalRefunds,
    int TotalUnmatched);
