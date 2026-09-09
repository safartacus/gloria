namespace Gloria.Domain.Periods;

public interface IPeriodRepository
{
    Task<Period?> GetAsync(int year, int month, CancellationToken ct = default);
    Task<Period> GetOrOpenAsync(int year, int month, DateTime now, CancellationToken ct = default);
    Task<IReadOnlyList<Period>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Period period, CancellationToken ct = default);
}
