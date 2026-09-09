using Gloria.Application.Abstractions;
using Gloria.Application.Imports.Adapters;
using Gloria.Domain.Imports;
using Gloria.Domain.Periods;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Sales;
using Microsoft.Extensions.Logging;

namespace Gloria.Application.Imports;

/// <summary>
/// Orchestrates the nightly "end-of-day" import: every source file is processed under one
/// <see cref="ImportBatch"/>. Idempotent via the natural key (source system + transaction no);
/// bad rows are quarantined in <see cref="ImportError"/> and never stop the batch; ERP rows that
/// mirror a PMS posting are reconciled away to avoid double counting; sales that fall into a
/// closed period are rejected.
/// </summary>
public sealed class EndOfDayImportService
{
    private readonly ICsvFileReader _csv;
    private readonly IReadOnlyDictionary<string, ISourceAdapter> _adapters;
    private readonly ISourceSystemRepository _sourceSystems;
    private readonly IEmployeeRepository _employees;
    private readonly ISaleRecordRepository _sales;
    private readonly IPeriodRepository _periods;
    private readonly IImportBatchRepository _batches;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<EndOfDayImportService> _logger;

    public EndOfDayImportService(
        ICsvFileReader csv,
        IEnumerable<ISourceAdapter> adapters,
        ISourceSystemRepository sourceSystems,
        IEmployeeRepository employees,
        ISaleRecordRepository sales,
        IPeriodRepository periods,
        IImportBatchRepository batches,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<EndOfDayImportService> logger)
    {
        _csv = csv;
        _adapters = adapters.ToDictionary(a => a.SourceSystemCode, StringComparer.OrdinalIgnoreCase);
        _sourceSystems = sourceSystems;
        _employees = employees;
        _sales = sales;
        _periods = periods;
        _batches = batches;
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<EndOfDayImportResult> RunAsync(
        string? businessDay, IReadOnlyList<SourceFileInput> sources, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var runBy = _currentUser.IsAuthenticated ? _currentUser.Username : "system";
        var day = string.IsNullOrWhiteSpace(businessDay) ? now.ToString("yyyy-MM-dd") : businessDay.Trim();

        var batch = ImportBatch.Start(day, "End-of-day sales import", runBy, now);
        await _batches.AddAsync(batch, ct);
        await _uow.SaveChangesAsync(ct);   // materialize batch.Id

        var sourceIdByCode = (await _sourceSystems.GetAllAsync(ct)).ToDictionary(s => s.Code, s => s.Id, StringComparer.OrdinalIgnoreCase);
        var employeeByNo = (await _employees.GetAllAsync(ct)).ToDictionary(e => e.EmployeeNo, e => e, StringComparer.OrdinalIgnoreCase);
        var closedPeriods = (await _periods.GetAllAsync(ct)).Where(p => !p.IsOpen).Select(p => (p.Year, p.Month)).ToHashSet();
        var existingKeys = await _sales.GetExistingKeysAsync(ct);
        var seenKeys = new HashSet<string>();
        var newSales = new List<SaleRecord>();
        var touchedPeriods = new HashSet<(int Year, int Month)>();

        try
        {
            foreach (var source in sources)
            {
                if (!sourceIdByCode.TryGetValue(source.SourceCode, out var sourceSystemId) ||
                    !_adapters.TryGetValue(source.SourceCode, out var adapter))
                {
                    batch.AddError(source.SourceCode, 0, source.DisplayName, ImportErrorCodes.SourceNotRegistered,
                        $"Source system '{source.SourceCode}' is not registered or has no adapter.", now);
                    continue;
                }

                var sourceCode = source.SourceCode;
                foreach (var row in source.ReadRows(_csv))
                {
                    ct.ThrowIfCancellationRequested();
                    batch.RowRead();

                    RowOutcome outcome;
                    try
                    {
                        outcome = adapter.Map(row);
                    }
                    catch (Exception ex)
                    {
                        batch.AddError(sourceCode, row.LineNumber, row.Raw, ImportErrorCodes.ParseError, ex.Message, now);
                        continue;
                    }

                    switch (outcome.Kind)
                    {
                        case RowOutcomeKind.Reconciled:
                            batch.RowReconciled();
                            continue;

                        case RowOutcomeKind.Error:
                            batch.AddError(sourceCode, row.LineNumber, row.Raw, outcome.ErrorCode!, outcome.ErrorMessage!, now);
                            continue;

                        case RowOutcomeKind.Sale:
                            HandleSale(batch, outcome.Sale!, sourceCode, sourceSystemId, row, employeeByNo,
                                closedPeriods, existingKeys, seenKeys, newSales, touchedPeriods, now);
                            continue;
                    }
                }
            }

            await _sales.AddRangeAsync(newSales, ct);
            batch.Complete(now);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "End-of-day import batch {BatchId} failed", batch.Id);
            batch.Fail(_clock.UtcNow);
            await _uow.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        foreach (var error in batch.Errors)
            _logger.LogWarning(
                "Import row quarantined — batch {BatchId}, {ImportSource} line {ImportLine}: {ImportErrorCode} — {ImportErrorMessage}. Raw: {ImportRawRow}",
                batch.Id, error.SourceSystemCode, error.RowNumber, error.ErrorCode, error.ErrorMessage, error.RawContent);

        if (batch.RowsFailed > 0)
            _logger.LogWarning(
                "Import batch {BatchId} ({BusinessDay}) finished with {RowsFailed} quarantined row(s) of {RowsRead} read.",
                batch.Id, batch.BusinessDay, batch.RowsFailed, batch.RowsRead);

        return new EndOfDayImportResult(
            batch.Id, batch.BusinessDay, batch.Status,
            batch.RowsRead, batch.RowsInserted, batch.RowsDuplicate, batch.RowsFailed, batch.RowsReconciled,
            batch.Errors.Select(e => new ImportErrorDto(e.SourceSystemCode, e.RowNumber, e.ErrorCode, e.ErrorMessage, e.RawContent)).ToList(),
            touchedPeriods.OrderBy(p => p.Year).ThenBy(p => p.Month).Select(p => new AffectedPeriod(p.Year, p.Month)).ToList());
    }

    private static void HandleSale(
        ImportBatch batch, CandidateSale sale, string sourceCode, int sourceSystemId, Abstractions.CsvRow row,
        IReadOnlyDictionary<string, Employee> employeeByNo, HashSet<(int, int)> closedPeriods,
        HashSet<string> existingKeys, HashSet<string> seenKeys, List<SaleRecord> newSales,
        HashSet<(int Year, int Month)> touchedPeriods, DateTime now)
    {
        if (!employeeByNo.TryGetValue(sale.EmployeeNo, out var employee))
        {
            batch.AddError(sourceCode, row.LineNumber, row.Raw, ImportErrorCodes.EmployeeNotFound,
                $"Employee '{sale.EmployeeNo}' not found.", now);
            return;
        }

        touchedPeriods.Add((sale.TransactionDate.Year, sale.TransactionDate.Month));

        var key = $"{sourceSystemId}|{sale.SourceTransactionNo}";
        if (existingKeys.Contains(key) || !seenKeys.Add(key))
        {
            batch.RowSkippedDuplicate();
            return;
        }

        if (closedPeriods.Contains((sale.TransactionDate.Year, sale.TransactionDate.Month)))
        {
            batch.AddError(sourceCode, row.LineNumber, row.Raw, ImportErrorCodes.PeriodClosed,
                $"Transaction date {sale.TransactionDate:yyyy-MM-dd} falls into a closed period.", now);
            return;
        }

        var record = SaleRecord.Create(
            sourceSystemId, sale.SourceTransactionNo, employee.Id, employee.DepartmentId,
            sale.ProductCode, new Money(sale.Amount, sale.Currency), sale.TransactionDate, sale.IsRefund,
            sale.OriginalSourceTransactionNo, batch.Id, sale.RawRecordJson, now);

        newSales.Add(record);
        batch.RowInserted();
    }
}
