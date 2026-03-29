using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfAccountTokenRepository(VpnPortalDbContext dbContext) : IAccountTokenRepository
{
    public async Task<AccountToken> AddAsync(AccountToken token, CancellationToken cancellationToken)
    {
        var entity = new AccountTokenEntity();
        entity.ApplyFromDomain(token);
        await dbContext.AccountTokens.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task<AccountToken?> GetByHashAsync(string tokenHash, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var purposeValue = purpose switch
        {
            AccountTokenPurpose.AccountActivation => "account_activation",
            AccountTokenPurpose.IpConfirmation => "ip_confirmation",
            AccountTokenPurpose.PasswordReset => "password_reset",
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };

        var entity = await dbContext.AccountTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash && x.Purpose == purposeValue)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<AccountToken?> GetLatestByEmailAsync(string email, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var purposeValue = purpose switch
        {
            AccountTokenPurpose.AccountActivation => "account_activation",
            AccountTokenPurpose.IpConfirmation => "ip_confirmation",
            AccountTokenPurpose.PasswordReset => "password_reset",
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };

        var entity = await dbContext.AccountTokens
            .AsNoTracking()
            .Where(x => x.UserEmail == email && x.Purpose == purposeValue)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomain();
    }

    public async Task UpdateAsync(AccountToken token, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AccountTokens.SingleAsync(x => x.Id == token.Id, cancellationToken);
        entity.ApplyFromDomain(token);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
