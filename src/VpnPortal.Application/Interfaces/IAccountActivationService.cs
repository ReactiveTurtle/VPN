using VpnPortal.Application.Contracts.Account;

namespace VpnPortal.Application.Interfaces;

public interface IAccountActivationService
{
    Task<ActivationTokenStatusDto> GetStatusAsync(string token, CancellationToken cancellationToken);
    Task<ActivationCompletedDto?> ActivateAsync(ActivateAccountCommand command, CancellationToken cancellationToken);
}
