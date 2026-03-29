using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfRequestRepository(VpnPortalDbContext dbContext) : IRequestRepository
{
    public async Task<VpnRequest?> GetLatestPendingByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnRequests
            .AsNoTracking()
            .Where(x => x.Email == email && x.Status == "pending")
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnRequest> AddAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        var entity = new VpnRequestEntity();
        entity.ApplyFromDomain(request);
        await dbContext.VpnRequests.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task<IReadOnlyCollection<VpnRequest>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.VpnRequests
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDomain()).ToArray();
    }

    public async Task<VpnRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task UpdateAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnRequests.SingleAsync(x => x.Id == request.Id, cancellationToken);
        entity.ApplyFromDomain(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
