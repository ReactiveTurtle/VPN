using Microsoft.Extensions.Options;
using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class VpnOnboardingInstructionService(IOptions<VpnAccessOptions> vpnAccessOptions) : IVpnOnboardingInstructionService
{
    private static readonly string[] SupportedPlatforms = ["ios", "android", "windows", "macos"];

    public VpnOnboardingInstructionDto Create(string platform, string vpnUsername)
    {
        var normalized = NormalizePlatform(platform);
        var serverAddress = string.IsNullOrWhiteSpace(vpnAccessOptions.Value.ServerAddress)
            ? "vpn.example.com"
            : vpnAccessOptions.Value.ServerAddress.Trim();

        return normalized switch
        {
            "ios" => new VpnOnboardingInstructionDto(
                "ios",
                "iPhone or iPad manual IKEv2 setup",
                "Create a native IKEv2 VPN profile and sign in with the device credential shown below.",
                [
                    $"Open Settings -> VPN -> Add VPN Configuration -> Type: IKEv2.",
                    $"Server: {serverAddress}.",
                    $"Remote ID: {serverAddress}. Local ID can stay empty unless your deployment requires one.",
                    $"Username: {vpnUsername}. Password: the device VPN password issued in the portal.",
                    "Save the profile and connect once to verify the tunnel comes up."
                ],
                "Use the device VPN username and password shown above."),
            "android" => new VpnOnboardingInstructionDto(
                "android",
                "Android IKEv2 setup",
                "Use the platform VPN settings or a compatible IKEv2 client and sign in with the issued device credential.",
                [
                    "Open Settings -> Network & Internet -> VPN and add a new profile, or use your approved IKEv2 client.",
                    $"Server: {serverAddress}. VPN type: IKEv2/IPSec with username and password.",
                    $"Username: {vpnUsername}. Password: the device VPN password issued in the portal.",
                    "Save the profile and connect. If your Android client requests IPSec identifiers, use the server address as Remote ID.",
                    "If your deployment later introduces QR or managed profiles, prefer those over manual entry."
                ],
                "Use the device VPN username and password shown above."),
            "windows" => new VpnOnboardingInstructionDto(
                "windows",
                "Windows built-in VPN setup",
                "Create a native Windows VPN connection and authenticate with the issued device credential.",
                [
                    "Open Settings -> Network & Internet -> VPN -> Add VPN.",
                    "VPN provider: Windows (built-in). Connection name: any descriptive name.",
                    $"Server name or address: {serverAddress}. VPN type: IKEv2.",
                    "Type of sign-in info: Username and password.",
                    $"Username: {vpnUsername}. Password: the device VPN password issued in the portal.",
                    "Save the profile, then connect from the Windows VPN settings screen."
                ],
                "Use the device VPN username and password shown above."),
            _ => new VpnOnboardingInstructionDto(
                "macos",
                "macOS native IKEv2 setup",
                "Create a native IKEv2 VPN profile in macOS and authenticate with the issued device credential.",
                [
                    "Open System Settings -> VPN -> Add VPN Configuration -> IKEv2.",
                    $"Server address: {serverAddress}. Remote ID: {serverAddress}.",
                    $"Username: {vpnUsername}. Password: the device VPN password issued in the portal.",
                    "Leave Local ID empty unless your deployment requires a specific value.",
                    "Save the profile and connect once to verify access."
                ],
                "Use the device VPN username and password shown above.")
        };
    }

    public IReadOnlyCollection<VpnOnboardingInstructionDto> CreateCatalog()
    {
        return SupportedPlatforms.Select(platform => Create(platform, "<device-username>")).ToArray();
    }

    private static string NormalizePlatform(string platform)
    {
        var normalized = platform.Trim().ToLowerInvariant();
        return normalized switch
        {
            "iphone" or "ipad" => "ios",
            "mac" or "osx" => "macos",
            _ => normalized
        };
    }
}
