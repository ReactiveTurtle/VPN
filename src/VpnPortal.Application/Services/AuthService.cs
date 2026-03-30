using VpnPortal.Application.Contracts.Auth;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ISuperAdminRepository superAdminRepository,
    IPasswordHasher passwordHasher,
    IAuditService auditService) : IAuthService
{
    public async Task<SessionUserDto?> AuthenticateUserAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameOrEmailAsync(command.Login, cancellationToken);
        if (user is null || !user.Active || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return null;
        }

        user.MarkLogin(DateTimeOffset.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);
        await auditService.WriteAsync("user", user.Id, "user_login", "vpn_user", user.Id.ToString(), null, new { user.Username }, cancellationToken);
        return new SessionUserDto(user.Id, user.Username, "User", user.Email);
    }

    public async Task<SessionUserDto?> AuthenticateSuperAdminAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var admin = await superAdminRepository.GetByUsernameAsync(command.Login, cancellationToken);
        if (admin is null || string.IsNullOrWhiteSpace(admin.PasswordHash))
        {
            return null;
        }

        if (!passwordHasher.Verify(command.Password, admin.PasswordHash))
        {
            return null;
        }

        admin.MarkLogin(DateTimeOffset.UtcNow);
        await superAdminRepository.UpdateAsync(admin, cancellationToken);
        await auditService.WriteAsync("superadmin", admin.Id, "superadmin_login", "superadmin", admin.Id.ToString(), null, new { admin.Username }, cancellationToken);
        return new SessionUserDto(admin.Id, admin.Username, "SuperAdmin", null);
    }
}
