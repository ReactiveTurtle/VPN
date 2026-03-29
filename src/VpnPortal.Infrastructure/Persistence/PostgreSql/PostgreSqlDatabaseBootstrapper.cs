using System.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlDatabaseBootstrapper(
    IHostEnvironment environment,
    IOptions<DatabaseOptions> options,
    ILogger<PostgreSqlDatabaseBootstrapper> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(options.Value.Provider, "PostgreSql", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(options.Value.ConnectionString) ||
            !options.Value.InitializeOnStartup)
        {
            return;
        }

        var root = environment.ContentRootPath;
        var schemaPath = Path.Combine(root, "..", "..", "database", "001_schema.sql");
        var seedPath = Path.Combine(root, "..", "..", "database", "002_seed_dev.sql");

        await ExecuteFileAsync(schemaPath, cancellationToken);

        if (options.Value.SeedDemoData && File.Exists(seedPath))
        {
            await ExecuteFileAsync(seedPath, cancellationToken);
        }
    }

    private async Task ExecuteFileAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("SQL bootstrap file not found: {Path}", path);
            return;
        }

        var sql = await File.ReadAllTextAsync(path, cancellationToken);
        await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Applied SQL bootstrap file {Path}", path);
    }
}
