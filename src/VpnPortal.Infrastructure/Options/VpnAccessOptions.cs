namespace VpnPortal.Infrastructure.Options;

public sealed class VpnAccessOptions
{
    public const string SectionName = "VpnAccess";

    public string ServerAddress { get; set; } = string.Empty;
}
