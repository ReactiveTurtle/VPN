using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Persistence.Ef;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
{
    throw new InvalidOperationException("Database:ConnectionString must be configured for migrations.");
}

builder.Services.AddDbContext<VpnPortalDbContext>(options => options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly("VpnPortal.Migrations")));

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<VpnPortalDbContext>();
await dbContext.Database.MigrateAsync();
