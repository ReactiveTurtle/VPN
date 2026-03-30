using VpnPortal.Application.Contracts.Account;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Application.Services;

public sealed class AccountActivationService(
    IAccountTokenRepository accountTokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProtector tokenProtector,
    IAuditService auditService) : IAccountActivationService
{
    public async Task<ActivationTokenStatusDto> GetStatusAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ActivationTokenStatusDto(false, false, null, null, "Токен активации отсутствует.");
        }

        var tokenHash = tokenProtector.Hash(token);
        var accountToken = await accountTokenRepository.GetByHashAsync(tokenHash, AccountTokenPurpose.AccountActivation, cancellationToken);
        if (accountToken is null)
        {
            return new ActivationTokenStatusDto(false, false, null, null, "Токен активации не найден.");
        }

        if (accountToken.Used)
        {
            return new ActivationTokenStatusDto(false, true, accountToken.UserEmail, accountToken.ExpiresAt, "Ссылка активации уже была использована.");
        }

        if (accountToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new ActivationTokenStatusDto(false, false, accountToken.UserEmail, accountToken.ExpiresAt, "Срок действия ссылки активации истек.");
        }

        return new ActivationTokenStatusDto(true, false, accountToken.UserEmail, accountToken.ExpiresAt, "Ссылка активации действительна.");
    }

    public async Task<ActivationCompletedDto?> ActivateAsync(ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 10)
        {
            return null;
        }

        var status = await GetStatusAsync(command.Token, cancellationToken);
        if (!status.Valid || string.IsNullOrWhiteSpace(status.Email))
        {
            return null;
        }

        var tokenHash = tokenProtector.Hash(command.Token);
        var accountToken = await accountTokenRepository.GetByHashAsync(tokenHash, AccountTokenPurpose.AccountActivation, cancellationToken);
        if (accountToken is null)
        {
            return null;
        }

        var user = await userRepository.GetByEmailAsync(status.Email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.ActivateAccount(passwordHasher.Hash(command.Password));
        await userRepository.UpdateAsync(user, cancellationToken);

        accountToken.Used = true;
        accountToken.UsedAt = DateTimeOffset.UtcNow;
        await accountTokenRepository.UpdateAsync(accountToken, cancellationToken);
        await auditService.WriteAsync("user", user.Id, "account_activated", "vpn_user", user.Id.ToString(), null, new { user.Email }, cancellationToken);

        return new ActivationCompletedDto(user.Id, user.Email, user.Username, "Учетная запись активирована. Теперь вы можете войти в портал и завершить настройку VPN.");
    }
}
