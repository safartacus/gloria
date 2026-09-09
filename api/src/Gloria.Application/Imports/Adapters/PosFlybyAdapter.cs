using Gloria.Application.Abstractions;
using Gloria.Domain.Imports;
using Gloria.Domain.Reference;
using static Gloria.Application.Imports.Adapters.AdapterHelpers;

namespace Gloria.Application.Imports.Adapters;

/// <summary>
/// POS (Flyby) outlet retail add-ons. Natural key: FisNo. Salesperson: SatisPersoneli.
/// IadeMi = "E" marks a refund. Product code is composed as "OUTLET-PLU" so scope patterns
/// like "SPA01-%" work.
/// </summary>
public sealed class PosFlybyAdapter : ISourceAdapter
{
    public string SourceSystemCode => SourceSystemCodes.Pos;

    public RowOutcome Map(CsvRow row)
    {
        var receiptNo = row.Get("FisNo");
        var employeeNo = row.Get("SatisPersoneli");
        var outlet = row.Get("OutletKodu");
        var plu = row.Get("PLU");
        if (string.IsNullOrWhiteSpace(receiptNo) || string.IsNullOrWhiteSpace(employeeNo) ||
            string.IsNullOrWhiteSpace(outlet) || string.IsNullOrWhiteSpace(plu))
            return RowOutcome.ForError(ImportErrorCodes.RequiredFieldMissing, "FisNo / SatisPersoneli / OutletKodu / PLU is required.");

        if (!TryParseDateTime(row.Get("Tarih"), row.Get("Saat"), out var date))
            return RowOutcome.ForError(ImportErrorCodes.InvalidDate, $"Unparseable Tarih/Saat '{row.Get("Tarih")} {row.Get("Saat")}'.");

        if (!TryParseAmount(row.Get("ToplamTutar"), out var amount))
            return RowOutcome.ForError(ImportErrorCodes.InvalidAmount, $"Unparseable ToplamTutar '{row.Get("ToplamTutar")}'.");

        var isRefund = string.Equals(row.Get("IadeMi"), "E", StringComparison.OrdinalIgnoreCase) || amount < 0;

        return RowOutcome.ForSale(new CandidateSale(
            SourceSystemCode, receiptNo.Trim(), employeeNo.Trim(), $"{outlet.Trim()}-{plu.Trim()}",
            Math.Abs(amount), "TRY", date, isRefund,
            OriginalSourceTransactionNo: null, RawJson(row)));
    }
}
