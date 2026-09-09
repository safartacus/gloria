using Gloria.Application.Abstractions;
using Gloria.Application.Common;
using Gloria.Domain.Calculations;
using Gloria.Domain.CommissionRules;
using Gloria.Domain.CommissionRules.Engine;
using Gloria.Domain.Common;
using Gloria.Domain.Periods;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Sales;

namespace Gloria.Application.Commissions;

/// <summary>
/// Calculates commission for a period (optionally a single employee): runs the pure engine over the
/// period's sales and effective rules, then persists a <see cref="CommissionResult"/> with its full
/// step-by-step trace. Recalculation is only allowed while the period is open.
/// </summary>
public sealed class CommissionCalculationService
{
    private readonly CommissionEngine _engine;
    private readonly ISaleRecordRepository _sales;
    private readonly ICommissionRuleRepository _rules;
    private readonly ICommissionResultRepository _results;
    private readonly IPeriodRepository _periods;
    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly ISourceSystemRepository _sourceSystems;
    private readonly ICacheStore _cache;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CommissionCalculationService(
        CommissionEngine engine, ISaleRecordRepository sales, ICommissionRuleRepository rules,
        ICommissionResultRepository results, IPeriodRepository periods, IEmployeeRepository employees,
        IDepartmentRepository departments, ISourceSystemRepository sourceSystems, ICacheStore cache,
        IUnitOfWork uow, ICurrentUser currentUser, IClock clock)
    {
        _engine = engine;
        _sales = sales;
        _rules = rules;
        _results = results;
        _periods = periods;
        _employees = employees;
        _departments = departments;
        _sourceSystems = sourceSystems;
        _cache = cache;
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CommissionResultDto>> CalculateAsync(CommissionCalculationRequest request, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var period = await _periods.GetOrOpenAsync(request.Year, request.Month, now, ct);
        if (!period.IsOpen)
            throw new DomainException($"Period {period.Year}-{period.Month:D2} is closed; it cannot be recalculated.");

        // Materialize a freshly opened period so its Id is available for the result's FK.
        var periodWasOpened = period.IsTransient;
        if (periodWasOpened)
            await _uow.SaveChangesAsync(ct);

        var employees = (await _employees.GetAllAsync(ct)).ToDictionary(e => e.Id);
        var departmentCodeById = (await _departments.GetAllAsync(ct)).ToDictionary(d => d.Id, d => d.Code);
        var sourceCodeById = (await _sourceSystems.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Code);
        var rules = await _rules.GetRulesEffectiveInPeriodAsync(request.Year, request.Month, ct);

        var sales = request.EmployeeId is int empId
            ? await _sales.GetByPeriodAndEmployeeAsync(request.Year, request.Month, empId, ct)
            : await _sales.GetByPeriodAsync(request.Year, request.Month, ct);

        var byEmployee = sales.GroupBy(s => s.EmployeeId);
        var output = new List<CommissionResultDto>();

        foreach (var group in byEmployee)
        {
            var inputs = group.Select(s => new CommissionInput(
                s.Id,
                sourceCodeById.GetValueOrDefault(s.SourceSystemId, "?"),
                s.ProductCode,
                departmentCodeById.GetValueOrDefault(s.DepartmentId, "?"),
                s.Amount.Amount,
                DateOnly.FromDateTime(s.TransactionDate),
                s.IsRefund)).ToList();

            var trace = _engine.Calculate(inputs, rules);

            var existing = await _results.GetAsync(period.Id, group.Key, ct);
            if (existing is null)
            {
                existing = CommissionResult.Create(period.Id, group.Key, trace, _currentUser.Username, now);
                await _results.AddAsync(existing, ct);
            }
            else
            {
                existing.Recalculate(trace, now);
            }

            output.Add(ToDto(existing, period, employees.GetValueOrDefault(group.Key)));
        }

        await _uow.SaveChangesAsync(ct);
        await _cache.BumpGenerationAsync(CacheKeys.CommissionGen(request.Year, request.Month), ct);
        if (periodWasOpened)
            await _cache.BumpGenerationAsync(CacheKeys.PeriodsGen, ct); // a new period now exists — refresh the periods list
        return output;
    }

    public async Task<CommissionResultDto?> GetAsync(int year, int month, int employeeId, CancellationToken ct = default)
    {
        var gen = await _cache.GetGenerationAsync(CacheKeys.CommissionGen(year, month), ct);
        return await _cache.GetOrCreateAsync(
            CacheKeys.CommissionEmployee(year, month, gen, employeeId), CacheKeys.CommissionTtl,
            async () =>
            {
                var period = await _periods.GetAsync(year, month, ct);
                if (period is null) return null;

                var result = await _results.GetAsync(period.Id, employeeId, ct);
                if (result is null) return null;

                var employee = await _employees.GetByIdAsync(employeeId, ct);
                return ToDto(result, period, employee);
            },
            ct);
    }

    /// <summary>One sorted, paged page of a period's per-employee commission list (no step traces).</summary>
    public async Task<PagedResult<CommissionListItemDto>> GetByPeriodPageAsync(CommissionPageQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var sortBy = query.SortBy ?? "totalCommission";
        var sortDir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var gen = await _cache.GetGenerationAsync(CacheKeys.CommissionGen(query.Year, query.Month), ct);
        return await _cache.GetOrCreateAsync(
            CacheKeys.CommissionPage(query.Year, query.Month, gen, page, pageSize, sortBy, sortDir), CacheKeys.CommissionTtl,
            () => LoadPageAsync(query.Year, query.Month, page, pageSize, sortBy, sortDir == "desc", ct),
            ct);
    }

    private async Task<PagedResult<CommissionListItemDto>> LoadPageAsync(
        int year, int month, int page, int pageSize, string sortBy, bool descending, CancellationToken ct)
    {
        var period = await _periods.GetAsync(year, month, ct);
        if (period is null) return PagedResult<CommissionListItemDto>.Empty(page, pageSize);

        var (rows, total) = await _results.GetByPeriodPageAsync(
            period.Id, (page - 1) * pageSize, pageSize, sortBy, descending, ct);

        var employees = (await _employees.GetAllAsync(ct)).ToDictionary(e => e.Id);
        var items = rows
            .Select(r =>
            {
                var e = employees.GetValueOrDefault(r.EmployeeId);
                return new CommissionListItemDto(
                    period.Id, period.Year, period.Month, period.Status,
                    r.EmployeeId, e?.EmployeeNo ?? "?", e?.FullName ?? "?",
                    r.TotalCommission, r.SaleCount, r.RefundCount, r.UnmatchedCount,
                    r.Status, r.CalculatedAt, r.CalculatedByUsername);
            })
            .ToList();

        return new PagedResult<CommissionListItemDto>(items, page, pageSize, total);
    }

    public async Task<PeriodCommissionSummary> GetPeriodSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        var gen = await _cache.GetGenerationAsync(CacheKeys.CommissionGen(year, month), ct);
        return await _cache.GetOrCreateAsync(
            CacheKeys.CommissionSummary(year, month, gen), CacheKeys.CommissionTtl,
            async () =>
            {
                var period = await _periods.GetAsync(year, month, ct);
                if (period is null) return new PeriodCommissionSummary(0, 0m, 0, 0);

                var totals = await _results.GetPeriodTotalsAsync(period.Id, ct);
                return new PeriodCommissionSummary(totals.EmployeeCount, totals.TotalCommission, totals.RefundCount, totals.UnmatchedCount);
            },
            ct);
    }

    /// <summary>All of a period's results with full traces — used only where every step is needed.</summary>
    public async Task<IReadOnlyList<CommissionResultDto>> GetByPeriodAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _periods.GetAsync(year, month, ct);
        if (period is null) return Array.Empty<CommissionResultDto>();

        var results = await _results.GetByPeriodAsync(period.Id, ct);
        var employees = (await _employees.GetAllAsync(ct)).ToDictionary(e => e.Id);
        return results.Select(r => ToDto(r, period, employees.GetValueOrDefault(r.EmployeeId))).ToList();
    }

    private static CommissionResultDto ToDto(CommissionResult r, Period period, Employee? employee) => new(
        period.Id, period.Year, period.Month, period.Status,
        r.EmployeeId, employee?.EmployeeNo ?? "?", employee?.FullName ?? "?",
        r.TotalCommission, r.SaleCount, r.RefundCount, r.UnmatchedCount, r.Status, r.CalculatedAt, r.CalculatedByUsername,
        r.Steps.OrderBy(s => s.Order).Select(s => new CalculationStepDto(
            s.Order, s.SaleRecordId, s.CommissionRuleId, s.CommissionRuleVersion, s.RuleTypeCode, s.ProductCode,
            s.Base, s.AppliedRate, s.AppliedAmount, s.StepAmount, s.RunningTotal, s.IsRefund, s.Explanation)).ToList());
}
