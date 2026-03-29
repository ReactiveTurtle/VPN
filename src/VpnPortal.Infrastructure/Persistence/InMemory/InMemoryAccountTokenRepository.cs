using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAccountTokenRepository(InMemoryPortalStore store) : IAccountTokenRepository
{
    public Task<AccountToken> AddAsync(AccountToken token, CancellationToken cancellationToken)
    {
        var copy = Clone(token);
        copy.Id = store.AllocateTokenId();
        store.AccountTokens.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task<AccountToken?> GetByHashAsync(string tokenHash, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var token = store.AccountTokens
            .Where(x => x.TokenHash == tokenHash && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .Select(Clone)
            .FirstOrDefault();

        return Task.FromResult(token);
    }

    public Task<AccountToken?> GetLatestByEmailAsync(string email, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var token = store.AccountTokens
            .Where(x => x.UserEmail == email && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .Select(Clone)
            .FirstOrDefault();

        return Task.FromResult(token);
    }

    public Task UpdateAsync(AccountToken token, CancellationToken cancellationToken)
    {
        var current = store.AccountTokens.First(x => x.Id == token.Id);
        current.UserEmail = token.UserEmail;
        current.TokenHash = token.TokenHash;
        current.Purpose = token.Purpose;
        current.ExpiresAt = token.ExpiresAt;
        current.Used = token.Used;
        current.UsedAt = token.UsedAt;
        current.CreatedAt = token.CreatedAt;
        return Task.CompletedTask;
    }

    private static AccountToken Clone(AccountToken source)
    {
        return new AccountToken
        {
            Id = source.Id,
            UserEmail = source.UserEmail,
            TokenHash = source.TokenHash,
            Purpose = source.Purpose,
            ExpiresAt = source.ExpiresAt,
            Used = source.Used,
            UsedAt = source.UsedAt,
            CreatedAt = source.CreatedAt
        };
    }
}
