namespace VpnPortal.Application.Interfaces;

public interface IEmailService
{
    Task SendActivationLinkAsync(string email, string activationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task SendIpConfirmationLinkAsync(string email, string confirmationLink, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
