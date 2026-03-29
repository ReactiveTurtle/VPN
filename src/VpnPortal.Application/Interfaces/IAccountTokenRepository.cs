using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Application.Interfaces;

public interface IAccountTokenRepository
{
    Task<AccountToken> AddAsync(AccountToken token, CancellationToken cancellationToken);
    Task<AccountToken?> GetByHashAsync(string tokenHash, AccountTokenPurpose purpose, CancellationToken cancellationToken);
    Task<AccountToken?> GetLatestByEmailAsync(string email, AccountTokenPurpose purpose, CancellationToken cancellationToken);
    Task UpdateAsync(AccountToken token, CancellationToken cancellationToken);
}
