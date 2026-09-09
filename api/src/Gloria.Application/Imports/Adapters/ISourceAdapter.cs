using Gloria.Application.Abstractions;

namespace Gloria.Application.Imports.Adapters;

/// <summary>A source row projected to the canonical shape, before employee/department resolution.</summary>
public sealed record CandidateSale(
    string SourceSystemCode,
    string SourceTransactionNo,
    string EmployeeNo,
    string ProductCode,
    decimal Amount,
    string Currency,
    DateTime TransactionDate,
    bool IsRefund,
    string? OriginalSourceTransactionNo,
    string RawRecordJson);

public enum RowOutcomeKind
{
    Sale,
    Error,
    /// <summary>Row is an accounting mirror of a sale already counted from another system — skipped to avoid double counting.</summary>
    Reconciled
}

public sealed record RowOutcome
{
    public RowOutcomeKind Kind { get; init; }
    public CandidateSale? Sale { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static RowOutcome ForSale(CandidateSale sale) => new() { Kind = RowOutcomeKind.Sale, Sale = sale };
    public static RowOutcome ForError(string code, string message) => new() { Kind = RowOutcomeKind.Error, ErrorCode = code, ErrorMessage = message };
    public static RowOutcome ForReconciled() => new() { Kind = RowOutcomeKind.Reconciled };
}

/// <summary>Maps one raw CSV row of a specific source system into a <see cref="RowOutcome"/>.</summary>
public interface ISourceAdapter
{
    string SourceSystemCode { get; }
    RowOutcome Map(CsvRow row);
}
