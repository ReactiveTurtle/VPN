namespace VpnPortal.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Console";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@vpn-portal.local";
    public string FromName { get; set; } = "VPN Portal";
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";
}
