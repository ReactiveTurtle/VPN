using VpnPortal.Application.Contracts.Auth;

namespace VpnPortal.Application.Interfaces;

public interface IAuthService
{
    Task<SessionUserDto?> AuthenticateUserAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<SessionUserDto?> AuthenticateSuperAdminAsync(LoginCommand command, CancellationToken cancellationToken);
}
