using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VpnPortal.Infrastructure.Security;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Persistence.Ef;
using VpnPortal.Migrations;

if (args.Length > 0 && string.Equals(args[0], "hash-password", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
    {
        throw new InvalidOperationException("Usage: dotnet run --project src/VpnPortal.Migrations -- hash-password <plaintext-password>");
    }

    var passwordHasher = new Argon2PasswordHasher();
    Console.WriteLine(passwordHasher.Hash(args[1]));
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddConfiguration(ConfigurationLoader.Build(builder.Environment.EnvironmentName));

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
databaseOptions.ConnectionString = DatabaseConnectionStringValidator.EnsureValid(
    databaseOptions.ConnectionString,
    "Database:ConnectionString must be configured for migrations. Use `hash-password` if you only need an Argon2id hash.");

builder.Services.AddDbContext<VpnPortalDbContext>(options => options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly("VpnPortal.Migrations")));

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<VpnPortalDbContext>();
await dbContext.Database.MigrateAsync();
