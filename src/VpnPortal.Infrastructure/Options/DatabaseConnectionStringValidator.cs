using Npgsql;

namespace VpnPortal.Infrastructure.Options;

public static class DatabaseConnectionStringValidator
{
    private const string PlaceholderToken = "__APP_DATABASE_CONNECTION_STRING__";

    public static string EnsureValid(string? connectionString, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(missingMessage);
        }

        var trimmedConnectionString = connectionString.Trim();
        if (string.Equals(trimmedConnectionString, PlaceholderToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Database:ConnectionString is still set to the checked-in placeholder. Configure a real PostgreSQL connection string via environment-specific runtime secrets.");
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(trimmedConnectionString);
            return trimmedConnectionString;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("Database:ConnectionString is not a valid PostgreSQL connection string.", exception);
        }
    }
}
