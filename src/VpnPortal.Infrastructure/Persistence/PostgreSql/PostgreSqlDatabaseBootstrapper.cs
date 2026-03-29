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
    private const string MigrationHistoryTableName = "schema_migrations";

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
        var migrationsDirectory = Path.Combine(root, "..", "..", "database", "migrations");
        var seedPath = Path.Combine(root, "..", "..", "database", "002_seed_dev.sql");

        await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureMigrationHistoryTableAsync(connection, cancellationToken);
        await ApplyMigrationAsync(connection, "001_schema.sql", schemaPath, cancellationToken);
        await ApplyDirectoryMigrationsAsync(connection, migrationsDirectory, cancellationToken);

        if (options.Value.SeedDemoData && File.Exists(seedPath))
        {
            await ExecuteFileAsync(connection, seedPath, cancellationToken);
        }
    }

    private async Task EnsureMigrationHistoryTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $"""
            create table if not exists {MigrationHistoryTableName} (
                id bigserial primary key,
                migration_name varchar(255) not null unique,
                applied_at timestamptz not null default now()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ApplyDirectoryMigrationsAsync(NpgsqlConnection connection, string migrationsDirectory, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(migrationsDirectory))
        {
            logger.LogInformation("Migration directory not found, skipping: {Path}", migrationsDirectory);
            return;
        }

        var migrationFiles = Directory
            .EnumerateFiles(migrationsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var migrationPath in migrationFiles)
        {
            await ApplyMigrationAsync(connection, Path.GetFileName(migrationPath), migrationPath, cancellationToken);
        }
    }

    private async Task ApplyMigrationAsync(NpgsqlConnection connection, string migrationName, string path, CancellationToken cancellationToken)
    {
        if (await IsMigrationAppliedAsync(connection, migrationName, cancellationToken))
        {
            logger.LogInformation("Skipping already applied migration {MigrationName}", migrationName);
            return;
        }

        await ExecuteFileAsync(connection, path, cancellationToken);

        const string insertSql = """
            insert into schema_migrations (migration_name)
            values (@migration_name);
            """;

        await using var command = new NpgsqlCommand(insertSql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("migration_name", migrationName);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Recorded applied migration {MigrationName}", migrationName);
    }

    private async Task<bool> IsMigrationAppliedAsync(NpgsqlConnection connection, string migrationName, CancellationToken cancellationToken)
    {
        const string sql = """
            select exists(
                select 1
                from schema_migrations
                where migration_name = @migration_name
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("migration_name", migrationName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private async Task ExecuteFileAsync(NpgsqlConnection connection, string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("SQL bootstrap file not found: {Path}", path);
            return;
        }

        var sql = await File.ReadAllTextAsync(path, cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Applied SQL bootstrap file {Path}", path);
    }
}
