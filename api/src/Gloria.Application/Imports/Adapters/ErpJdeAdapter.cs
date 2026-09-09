using Gloria.Application.Abstractions;
using Gloria.Domain.Imports;
using Gloria.Domain.Reference;
using static Gloria.Application.Imports.Adapters.AdapterHelpers;

namespace Gloria.Application.Imports.Adapters;

/// <summary>
/// ERP (Oracle JDE) postings. Natural key: DocNumber. Employee: EmployeeNo.
/// <list type="bullet">
///   <item>DocType RI with Reference "PMS-*"  → reconciliation match, skipped (PMS already counts it).</item>
///   <item>DocType RI with no/other reference → ERP-origin sale.</item>
///   <item>DocType RM                          → refund; Reference points to the credited ERP doc.</item>
///   <item>Any other DocType                   → error (unknown record type).</item>
///   <item>Status != POSTED                    → error (not final).</item>
/// </list>
/// ERP has no product code, so one is derived from Description.
/// </summary>
public sealed class ErpJdeAdapter : ISourceAdapter
{
    public string SourceSystemCode => SourceSystemCodes.Erp;

    public RowOutcome Map(CsvRow row)
    {
        var docNo = row.Get("DocNumber");
        var employeeNo = row.Get("EmployeeNo");
        var docType = (row.Get("DocType") ?? string.Empty).Trim().ToUpperInvariant();
        var status = (row.Get("Status") ?? string.Empty).Trim().ToUpperInvariant();
        var reference = (row.Get("Reference") ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(docNo) || string.IsNullOrWhiteSpace(employeeNo))
            return RowOutcome.ForError(ImportErrorCodes.RequiredFieldMissing, "DocNumber / EmployeeNo is required.");

        if (docType is not ("RI" or "RM"))
            return RowOutcome.ForError(ImportErrorCodes.UnknownRecordType, $"Unsupported DocType '{docType}'.");

        if (status != "POSTED")
            return RowOutcome.ForError(ImportErrorCodes.UnpostedRecord, $"DocNumber {docNo} is '{status}', not POSTED.");

        if (docType == "RI" && reference.StartsWith("PMS-", StringComparison.OrdinalIgnoreCase))
            return RowOutcome.ForReconciled();

        if (!TryParseDate(row.Get("GLDate"), out var date))
            return RowOutcome.ForError(ImportErrorCodes.InvalidDate, $"Unparseable GLDate '{row.Get("GLDate")}'.");

        if (!TryParseAmount(row.Get("Amount"), out var amount))
            return RowOutcome.ForError(ImportErrorCodes.InvalidAmount, $"Unparseable Amount '{row.Get("Amount")}'.");

        var isRefund = docType == "RM" || amount < 0;
        var description = row.Get("Description");
        var productCode = string.IsNullOrWhiteSpace(description)
            ? $"ERP_{row.Get("ObjectAccount")}"
            : $"ERP_{Slug(description!)}";

        return RowOutcome.ForSale(new CandidateSale(
            SourceSystemCode, docNo.Trim(), employeeNo.Trim(), productCode,
            Math.Abs(amount),
            string.IsNullOrWhiteSpace(row.Get("Currency")) ? "TRY" : row.Get("Currency")!,
            date, isRefund,
            OriginalSourceTransactionNo: docType == "RM" && reference.Length > 0 ? reference : null,
            RawJson(row)));
    }
}
