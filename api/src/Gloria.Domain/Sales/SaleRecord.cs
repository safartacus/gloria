using Gloria.Domain.Common;

namespace Gloria.Domain.Sales;

/// <summary>
/// The canonical sale record imported from source systems. The amount is always stored gross/positive;
/// direction is expressed by <see cref="IsRefund"/>. Use <see cref="CommissionBase"/> for the signed
/// base that feeds commission calculation.
/// Natural key: (SourceSystemId, SourceTransactionNo) — prevents duplicate imports.
/// </summary>
public sealed class SaleRecord : AggregateRoot<long>, IAuditable
{
    public int SourceSystemId { get; private set; }
    public string SourceTransactionNo { get; private set; } = default!;
    public int EmployeeId { get; private set; }
    public int DepartmentId { get; private set; }
    public string ProductCode { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public DateTime TransactionDate { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool IsRefund { get; private set; }
    public string? OriginalSourceTransactionNo { get; private set; }
    public int ImportBatchId { get; private set; }
    public string RawRecordJson { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private SaleRecord() { }

    public static SaleRecord Create(
        int sourceSystemId,
        string sourceTransactionNo,
        int employeeId,
        int departmentId,
        string productCode,
        Money amount,
        DateTime transactionDate,
        bool isRefund,
        string? originalSourceTransactionNo,
        int importBatchId,
        string rawRecordJson,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(sourceTransactionNo)) throw new DomainException("Source transaction number is required.");
        if (string.IsNullOrWhiteSpace(productCode)) throw new DomainException("Product code is required.");
        if (amount.Amount < 0)
            throw new DomainException("Gross amount cannot be negative; refund direction is expressed by IsRefund.");

        return new SaleRecord
        {
            SourceSystemId = sourceSystemId,
            SourceTransactionNo = sourceTransactionNo.Trim(),
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            ProductCode = productCode.Trim().ToUpperInvariant(),
            Amount = amount.Abs(),
            TransactionDate = transactionDate,
            Year = transactionDate.Year,
            Month = transactionDate.Month,
            IsRefund = isRefund,
            OriginalSourceTransactionNo = string.IsNullOrWhiteSpace(originalSourceTransactionNo) ? null : originalSourceTransactionNo.Trim(),
            ImportBatchId = importBatchId,
            RawRecordJson = rawRecordJson,
            CreatedAt = now
        };
    }

    /// <summary>Signed base that feeds commission: negative when this is a refund.</summary>
    public decimal CommissionBase => IsRefund ? -Amount.Amount : Amount.Amount;
}
