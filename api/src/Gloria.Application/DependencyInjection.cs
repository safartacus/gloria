using Gloria.Application.Auth;
using Gloria.Application.Commissions;
using Gloria.Application.Imports;
using Gloria.Application.Imports.Adapters;
using Gloria.Application.Periods;
using Gloria.Application.Personnel;
using Gloria.Application.Rules;
using Gloria.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Gloria.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISourceAdapter, PmsFidelioAdapter>();
        services.AddScoped<ISourceAdapter, PosFlybyAdapter>();
        services.AddScoped<ISourceAdapter, ErpJdeAdapter>();

        services.AddScoped<AuthService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<EndOfDayImportService>();
        services.AddScoped<PersonnelSyncService>();
        services.AddScoped<CommissionCalculationService>();
        services.AddScoped<CommissionRuleService>();
        services.AddScoped<PeriodService>();

        return services;
    }
}
