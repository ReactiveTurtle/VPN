using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfIpChangeConfirmationRepository(VpnPortalDbContext dbContext) : IIpChangeConfirmationRepository
{
    public async Task<IReadOnlyCollection<IpChangeConfirmation>> GetPendingByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var entities = await dbContext.IpChangeConfirmations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDomain()).ToArray();
    }

    public async Task<IpChangeConfirmation> AddAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        var entity = new IpChangeConfirmationEntity();
        entity.ApplyFromDomain(confirmation);
        await dbContext.IpChangeConfirmations.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task<IpChangeConfirmation?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var entity = await dbContext.IpChangeConfirmations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task UpdateAsync(IpChangeConfirmation confirmation, CancellationToken cancellationToken)
    {
        var entity = await dbContext.IpChangeConfirmations.SingleAsync(x => x.Id == confirmation.Id, cancellationToken);
        entity.ApplyFromDomain(confirmation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
