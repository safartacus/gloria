using Gloria.Application.Abstractions;
using Gloria.Domain.Auditing;
using Gloria.Domain.Calculations;
using Gloria.Domain.CommissionRules;
using Gloria.Domain.CommissionRules.Engine;
using Gloria.Domain.Imports;
using Gloria.Domain.Periods;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Sales;
using Gloria.Domain.Users;
using Gloria.Infrastructure.Caching;
using Gloria.Infrastructure.Csv;
using Gloria.Infrastructure.Import;
using Gloria.Infrastructure.Persistence;
using Gloria.Infrastructure.Persistence.Interceptors;
using Gloria.Infrastructure.Persistence.Repositories;
using Gloria.Infrastructure.Security;
using Gloria.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using StackExchange.Redis;

namespace Gloria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Gloria")
            ?? throw new InvalidOperationException("Connection string 'Gloria' is not configured.");

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<CredentialFileOptions>(configuration.GetSection(CredentialFileOptions.SectionName));
        services.Configure<NightlyImportOptions>(configuration.GetSection(NightlyImportOptions.SectionName));
        services.Configure<PersonnelSyncOptions>(configuration.GetSection(PersonnelSyncOptions.SectionName));
        services.AddSingleton<PersonnelSyncSource>();
        services.AddScoped<NightlyBatchRunner>();                  // roster sync + folder import + recalculation
        services.AddHostedService<NightlyImportHostedService>();   // self-disables unless Enabled = true
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ICredentialFileWriter, TextCredentialFileWriter>();

        services.AddDbContext<GloriaDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(GloriaDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GloriaDbContext>());

        // Redis cache (required)
        var redisConnection = configuration.GetSection("Redis")["ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnection))
            throw new InvalidOperationException("Redis:ConnectionString is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheStore, RedisCacheStore>();

        // repositories
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISourceSystemRepository, SourceSystemRepository>();
        services.AddScoped<IRuleTypeRepository, RuleTypeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ISaleRecordRepository, SaleRecordRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<IPeriodRepository, PeriodRepository>();
        services.AddScoped<ICommissionResultRepository, CommissionResultRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IImportBatchRepository, ImportBatchRepository>();

        // read-through cache decorators (Tier 1 reference data).
        // Only repos whose entities are read for scalar lookups — never attached to a written aggregate.
        // (Role / RuleType are NOT cached: they become navigations on new User / CommissionRule aggregates,
        //  and a detached cached instance would make EF try to re-insert them.)
        services.Decorate<ISourceSystemRepository, CachedSourceSystemRepository>();
        services.Decorate<IDepartmentRepository, CachedDepartmentRepository>();
        services.Decorate<IEmployeeRepository, CachedEmployeeRepository>();

        // commission engine + handlers (pure domain services)
        services.AddSingleton<ICommissionRuleHandler, FixedPercentageHandler>();
        services.AddSingleton<ICommissionRuleHandler, FixedAmountHandler>();
        services.AddSingleton<ICommissionRuleHandler, TieredBracketHandler>();
        services.AddSingleton(sp => new CommissionEngine(sp.GetRequiredService<IEnumerable<ICommissionRuleHandler>>()));

        // csv
        services.AddSingleton<ICsvFileReader, SemicolonCsvFileReader>();

        return services;
    }
}
