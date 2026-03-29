using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class SmtpEmailService(IOptions<EmailOptions> options) : IEmailService
{
    public async Task SendActivationLinkAsync(string email, string activationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var publicLink = BuildPublicLink(activationLink, settings.PublicBaseUrl);
        var subject = "Your VPN account activation link";
        var body = $"""
            Hello,

            Your VPN access request has been approved.

            Use the activation link below to create your password:
            {publicLink}

            This link expires at {expiresAt:O}.

            If you did not request access, ignore this message.
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(email);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(settings.Username, settings.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    public async Task SendIpConfirmationLinkAsync(string email, string confirmationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var publicLink = BuildPublicLink(confirmationLink, settings.PublicBaseUrl);
        var subject = "Confirm a new VPN source IP";
        var body = $"""
            Hello,

            A VPN connection attempt was detected from a new IP address.

            Confirm this IP from the link below:
            {publicLink}

            This link expires at {expiresAt:O}.

            If this was not you, ignore this email and contact an administrator.
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(email);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(settings.Username, settings.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildPublicLink(string activationLink, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(activationLink))
        {
            return baseUrl;
        }

        return Uri.TryCreate(new Uri(AppendTrailingSlash(baseUrl)), activationLink.TrimStart('/'), out var uri)
            ? uri.ToString()
            : activationLink;
    }

    private static string AppendTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
}
