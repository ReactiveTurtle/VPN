namespace VpnPortal.Infrastructure.Options;

public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string SharedSecret { get; set; } = string.Empty;
}
