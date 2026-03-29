namespace VpnPortal.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "InMemory";
    public string? ConnectionString { get; set; }
    public bool InitializeOnStartup { get; set; }
    public bool SeedDemoData { get; set; } = true;
}
