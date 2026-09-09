namespace Gloria.Domain.Sales;

public interface ISaleRecordRepository
{
    /// <summary>Has this natural key (source + transaction no) already been imported? Duplicate check.</summary>
    Task<bool> ExistsAsync(int sourceSystemId, string sourceTransactionNo, CancellationToken ct = default);

    /// <summary>Existing "sourceSystemId|transactionNo" keys — for bulk duplicate checks during import.</summary>
    Task<HashSet<string>> GetExistingKeysAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SaleRecord>> GetByPeriodAndEmployeeAsync(int year, int month, int employeeId, CancellationToken ct = default);

    Task<IReadOnlyList<SaleRecord>> GetByPeriodAsync(int year, int month, CancellationToken ct = default);

    Task AddAsync(SaleRecord sale, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<SaleRecord> sales, CancellationToken ct = default);
}
