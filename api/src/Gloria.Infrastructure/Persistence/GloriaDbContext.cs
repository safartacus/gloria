using Gloria.Application.Abstractions;
using Gloria.Domain.Auditing;
using Gloria.Domain.Calculations;
using Gloria.Domain.CommissionRules;
using Gloria.Domain.Imports;
using Gloria.Domain.Periods;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Sales;
using Gloria.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Infrastructure.Persistence;

public sealed class GloriaDbContext : DbContext, IUnitOfWork
{
    public GloriaDbContext(DbContextOptions<GloriaDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SourceSystem> SourceSystems => Set<SourceSystem>();
    public DbSet<RuleType> RuleTypes => Set<RuleType>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<BracketTier> BracketTiers => Set<BracketTier>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<CommissionResult> CommissionResults => Set<CommissionResult>();
    public DbSet<CommissionResultStep> CommissionResultSteps => Set<CommissionResultStep>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportError> ImportErrors => Set<ImportError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GloriaDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no native decimal: store as REAL so numeric ordering / comparison work.
        // Money is rounded to 2dp in the domain before it is ever persisted.
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct) => base.SaveChangesAsync(ct);
}
