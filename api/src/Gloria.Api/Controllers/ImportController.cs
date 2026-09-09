using Gloria.Api.Configuration;
using Gloria.Application.Imports;
using Gloria.Domain.Imports;
using Gloria.Domain.Users;
using Gloria.Infrastructure.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Api.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Roles = RoleCodes.Admin)]
public sealed class ImportController : ControllerBase
{
    private readonly EndOfDayImportService _endOfDay;
    private readonly IImportBatchRepository _batches;
    private readonly SampleImportSource _sample;
    private readonly NightlyBatchRunner _nightlyBatch;

    public ImportController(
        EndOfDayImportService endOfDay, IImportBatchRepository batches, SampleImportSource sample, NightlyBatchRunner nightlyBatch)
    {
        _endOfDay = endOfDay;
        _batches = batches;
        _sample = sample;
        _nightlyBatch = nightlyBatch;
    }

    /// <summary>
    /// Run the end-of-day sales import over uploaded PMS/POS/ERP CSV files (multipart/form-data).
    /// Upload at least one file. Duplicates are skipped, bad rows quarantined, ERP↔PMS mirrors reconciled.
    /// </summary>
    [HttpPost("end-of-day")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    public async Task<ActionResult<EndOfDayImportResult>> ImportEndOfDay(
        [FromForm] string? businessDay,
        IFormFile? pms,
        IFormFile? pos,
        IFormFile? erp,
        CancellationToken ct)
    {
        var uploads = new[] { ("PMS", pms), ("POS", pos), ("ERP", erp) }
            .Where(x => x.Item2 is { Length: > 0 })
            .ToList();

        if (uploads.Count == 0)
            return BadRequest(new { title = "Dosya yok", detail = "En az bir CSV dosyası yükleyin (PMS / POS / ERP)." });

        var streams = new List<MemoryStream>();
        try
        {
            var sources = new List<SourceFileInput>();
            foreach (var (code, file) in uploads)
            {
                var ms = new MemoryStream();
                await file!.CopyToAsync(ms, ct);
                ms.Position = 0;
                streams.Add(ms);
                sources.Add(new SourceFileInput(code, file.FileName, ms));
            }

            return Ok(await _endOfDay.RunAsync(businessDay, sources, ct));
        }
        finally
        {
            foreach (var s in streams) s.Dispose();
        }
    }

    /// <summary>
    /// Run the end-of-day import over the bundled sample CSVs under <c>data/</c> (demo convenience).
    /// These files are read only here, never at startup. 400 if none are present on the server.
    /// </summary>
    [HttpPost("end-of-day/sample")]
    public async Task<ActionResult<EndOfDayImportResult>> ImportEndOfDaySample(CancellationToken ct)
    {
        var sources = _sample.Open();
        if (sources.Count == 0)
            return BadRequest(new { title = "Örnek dosya yok", detail = "Sunucuda data/ altında örnek CSV bulunamadı — dosyaları yükleyin." });

        try
        {
            return Ok(await _endOfDay.RunAsync(_sample.BusinessDay, sources, ct));
        }
        finally
        {
            foreach (var s in sources) s.Content.Dispose();
        }
    }

    /// <summary>
    /// Run the full nightly batch on demand (no waiting for the scheduled time): roster sync from
    /// <c>data/personel.csv</c>, import of the PMS/POS/ERP CSVs in the inbox / <c>data/</c> folder,
    /// then commission recalculation and a fresh summary for every period the import touched.
    /// Idempotent — safe to press repeatedly.
    /// </summary>
    [HttpPost("end-of-day/run-nightly")]
    public async Task<ActionResult<NightlyBatchResult>> RunNightlyBatch(CancellationToken ct) =>
        Ok(await _nightlyBatch.RunAsync(ct));

    /// <summary>Recent import batches with their counters.</summary>
    [HttpGet("batches")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<object>> RecentBatches([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var batches = await _batches.GetRecentAsync(count, ct);
        return Ok(batches.Select(b => new
        {
            b.Id, b.BusinessDay, b.Description, b.Status, b.StartedAt, b.FinishedAt,
            b.RowsRead, b.RowsInserted, b.RowsDuplicate, b.RowsFailed, b.RowsReconciled, b.RunByUsername
        }));
    }

    /// <summary>
    /// Every import batch (nightly job + manual) that ran on the given date, each with its counters
    /// and quarantined rows. Powers the admin "job errors by day" view. <paramref name="date"/> is the
    /// server's local calendar day, "yyyy-MM-dd".
    /// </summary>
    [HttpGet("batches/by-date")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<IReadOnlyList<ImportBatchLogDto>>> BatchesByDate([FromQuery] DateOnly date, CancellationToken ct)
    {
        var batches = await _batches.GetByRunDateAsync(date, ct);
        return Ok(batches.Select(b => new ImportBatchLogDto(
            b.Id, b.BusinessDay, b.Description, b.Status, b.StartedAt, b.FinishedAt, b.RunByUsername,
            b.RowsRead, b.RowsInserted, b.RowsDuplicate, b.RowsFailed, b.RowsReconciled,
            b.Errors
                .OrderBy(e => e.SourceSystemCode).ThenBy(e => e.RowNumber)
                .Select(e => new ImportErrorDto(e.SourceSystemCode, e.RowNumber, e.ErrorCode, e.ErrorMessage, e.RawContent))
                .ToList())).ToList());
    }

    /// <summary>One batch with its quarantined error rows.</summary>
    [HttpGet("batches/{id:int}")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Accounting}")]
    public async Task<ActionResult<object>> Batch(int id, CancellationToken ct)
    {
        var batch = await _batches.GetByIdAsync(id, ct);
        if (batch is null) return NotFound();
        return Ok(new
        {
            batch.Id, batch.BusinessDay, batch.Status,
            batch.RowsRead, batch.RowsInserted, batch.RowsDuplicate, batch.RowsFailed, batch.RowsReconciled,
            Errors = batch.Errors.Select(e => new { e.SourceSystemCode, e.RowNumber, e.ErrorCode, e.ErrorMessage, e.RawContent })
        });
    }
}
