using Gloria.Application.Abstractions;
using Gloria.Domain.Imports;
using Gloria.Domain.Reference;
using static Gloria.Application.Imports.Adapters.AdapterHelpers;

namespace Gloria.Application.Imports.Adapters;

/// <summary>
/// PMS (Fidelio) room postings. Natural key: BelgeNo. Salesperson: KasiyerNo.
/// IslemTipi = REVERSAL marks a refund (amount comes in negative).
/// </summary>
public sealed class PmsFidelioAdapter : ISourceAdapter
{
    public string SourceSystemCode => SourceSystemCodes.Pms;

    public RowOutcome Map(CsvRow row)
    {
        var docNo = row.Get("BelgeNo");
        var employeeNo = row.Get("KasiyerNo");
        var productCode = row.Get("UrunKodu");
        if (string.IsNullOrWhiteSpace(docNo) || string.IsNullOrWhiteSpace(employeeNo) || string.IsNullOrWhiteSpace(productCode))
            return RowOutcome.ForError(ImportErrorCodes.RequiredFieldMissing, "BelgeNo / KasiyerNo / UrunKodu is required.");

        if (!TryParseDate(row.Get("IslemTarihi"), out var date))
            return RowOutcome.ForError(ImportErrorCodes.InvalidDate, $"Unparseable IslemTarihi '{row.Get("IslemTarihi")}'.");

        if (!TryParseAmount(row.Get("Tutar"), out var amount))
            return RowOutcome.ForError(ImportErrorCodes.InvalidAmount, $"Unparseable Tutar '{row.Get("Tutar")}'.");

        var isRefund = string.Equals(row.Get("IslemTipi"), "REVERSAL", StringComparison.OrdinalIgnoreCase) || amount < 0;
        var currency = string.IsNullOrWhiteSpace(row.Get("ParaBirimi")) ? "TRY" : row.Get("ParaBirimi")!;

        return RowOutcome.ForSale(new CandidateSale(
            SourceSystemCode, docNo.Trim(), employeeNo.Trim(), productCode.Trim(),
            Math.Abs(amount), currency, date, isRefund,
            OriginalSourceTransactionNo: null, RawJson(row)));
    }
}
