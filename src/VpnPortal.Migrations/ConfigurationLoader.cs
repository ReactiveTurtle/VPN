using Microsoft.Extensions.Configuration;

namespace VpnPortal.Migrations;

internal static class ConfigurationLoader
{
    public static IConfigurationRoot Build(string environmentName, string? basePath = null)
    {
        var resolvedBasePath = ResolveBasePath(basePath);

        return new ConfigurationBuilder()
            .SetBasePath(resolvedBasePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveBasePath(string? basePath)
    {
        if (!string.IsNullOrWhiteSpace(basePath) && File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            return basePath;
        }

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
        {
            return AppContext.BaseDirectory;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "VpnPortal.Migrations");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return string.IsNullOrWhiteSpace(basePath) ? Directory.GetCurrentDirectory() : basePath;
    }
}
