namespace Gloria.Domain.CommissionRules.Engine;

/// <summary>
/// A read-only input to the engine, projected from a SaleRecord. The engine has no dependency on
/// infrastructure (EF, repositories) — it only knows this type.
/// </summary>
public sealed record CommissionInput(
    long SaleRecordId,
    string SourceSystemCode,
    string ProductCode,
    string DepartmentCode,
    decimal Amount,
    DateOnly TransactionDate,
    bool IsRefund)
{
    /// <summary>Negative when a refund, positive otherwise.</summary>
    public decimal CommissionBase => IsRefund ? -Math.Abs(Amount) : Math.Abs(Amount);
}
