using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Persistence.Ef;

namespace VpnPortal.Migrations;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VpnPortalDbContext>
{
    public VpnPortalDbContext CreateDbContext(string[] args)
    {
        var configuration = ConfigurationLoader.Build("Development", Directory.GetCurrentDirectory());

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString must be configured for design-time DbContext creation.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<VpnPortalDbContext>();
        optionsBuilder.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly("VpnPortal.Migrations"));
        return new VpnPortalDbContext(optionsBuilder.Options);
    }
}
