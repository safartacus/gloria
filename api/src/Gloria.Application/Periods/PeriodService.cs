using Gloria.Application.Abstractions;
using Gloria.Domain.Common;
using Gloria.Domain.Periods;

namespace Gloria.Application.Periods;

public sealed record PeriodDto(int Id, int Year, int Month, string Status, string? ClosedByUsername, DateTime? ClosedAt);

/// <summary>Lists periods and closes a completed one. Once closed, its records are immutable.</summary>
public sealed class PeriodService
{
    private readonly IPeriodRepository _periods;
    private readonly ICacheStore _cache;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public PeriodService(IPeriodRepository periods, ICacheStore cache, IUnitOfWork uow, ICurrentUser currentUser, IClock clock)
    {
        _periods = periods;
        _cache = cache;
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct = default)
    {
        var gen = await _cache.GetGenerationAsync(CacheKeys.PeriodsGen, ct);
        return await _cache.GetOrCreateAsync(
            CacheKeys.PeriodsList(gen), CacheKeys.PeriodTtl,
            async () => (await _periods.GetAllAsync(ct)).Select(ToDto).ToList(),
            ct);
    }

    public async Task<PeriodDto> CloseAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _periods.GetAsync(year, month, ct)
            ?? throw new DomainException($"Period {year}-{month:D2} does not exist yet; nothing to close.");

        period.Close(_currentUser.Username, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);

        await _cache.BumpGenerationAsync(CacheKeys.PeriodsGen, ct);
        await _cache.BumpGenerationAsync(CacheKeys.CommissionGen(year, month), ct); // list rows show periodStatus

        return ToDto(period);
    }

    public async Task<PeriodDto> OpenAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _periods.GetOrOpenAsync(year, month, _clock.UtcNow, ct);
        await _uow.SaveChangesAsync(ct);

        await _cache.BumpGenerationAsync(CacheKeys.PeriodsGen, ct);

        return ToDto(period);
    }

    private static PeriodDto ToDto(Period p) => new(p.Id, p.Year, p.Month, p.Status, p.ClosedByUsername, p.ClosedAt);
}
