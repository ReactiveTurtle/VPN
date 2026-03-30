using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;

namespace VpnPortal.Infrastructure.Services;

public sealed class ConsoleEmailService(IOptions<EmailOptions> options, ILogger<ConsoleEmailService> logger) : IEmailService
{
    public Task SendActivationLinkAsync(string email, string activationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var publicLink = BuildPublicLink(activationLink, options.Value.PublicBaseUrl);

        logger.LogInformation(
            "Activation email prepared for {Email}. Expires at {ExpiresAt}. Link: {ActivationLink}",
            email,
            expiresAt,
            publicLink);

        return Task.CompletedTask;
    }

    public Task SendIpConfirmationLinkAsync(string email, string confirmationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var publicLink = BuildPublicLink(confirmationLink, options.Value.PublicBaseUrl);

        logger.LogInformation(
            "IP confirmation email prepared for {Email}. Expires at {ExpiresAt}. Link: {ConfirmationLink}",
            email,
            expiresAt,
            publicLink);

        return Task.CompletedTask;
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
