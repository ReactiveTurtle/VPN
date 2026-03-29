namespace VpnPortal.Infrastructure.Options;

public sealed class VpnRuntimeOptions
{
    public const string SectionName = "VpnRuntime";

    public string DisconnectScriptPath { get; set; } = "/usr/local/lib/vpnportal/disconnect-session.sh";
}
