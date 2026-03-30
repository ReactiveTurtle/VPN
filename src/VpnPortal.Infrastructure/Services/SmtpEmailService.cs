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
        var subject = "Ссылка активации VPN-учетной записи";
        var body = $"""
            Здравствуйте,

            Ваша заявка на VPN-доступ одобрена.

            Перейдите по ссылке ниже, чтобы создать пароль:
            {publicLink}

            Ссылка действительна до {expiresAt:O}.

            Если вы не запрашивали доступ, просто проигнорируйте это письмо.
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
        var subject = "Подтвердите новый исходный IP-адрес VPN";
        var body = $"""
            Здравствуйте,

            Обнаружена попытка VPN-подключения с нового IP-адреса.

            Подтвердите этот IP-адрес по ссылке ниже:
            {publicLink}

            Ссылка действительна до {expiresAt:O}.

            Если это были не вы, проигнорируйте письмо и свяжитесь с администратором.
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

        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(AppendTrailingSlash(baseUrl), UriKind.Absolute, out var baseUri))
        {
            return activationLink;
        }

        return Uri.TryCreate(baseUri, activationLink.TrimStart('/'), out var uri)
            ? uri.ToString()
            : activationLink;
    }

    private static string AppendTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
}
