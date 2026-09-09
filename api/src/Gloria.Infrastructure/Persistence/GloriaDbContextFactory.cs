using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gloria.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef` can build the model without booting the API.
/// The runtime connection string comes from configuration in <see cref="DependencyInjection"/>.
/// </summary>
public sealed class GloriaDbContextFactory : IDesignTimeDbContextFactory<GloriaDbContext>
{
    public GloriaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GLORIA_CONNECTION_STRING")
            ?? "Data Source=gloria.db";

        var options = new DbContextOptionsBuilder<GloriaDbContext>()
            .UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(GloriaDbContext).Assembly.FullName))
            .Options;

        return new GloriaDbContext(options);
    }
}
