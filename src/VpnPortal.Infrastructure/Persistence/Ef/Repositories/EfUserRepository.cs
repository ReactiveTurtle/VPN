using Microsoft.EntityFrameworkCore;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;
using VpnPortal.Infrastructure.Persistence.Ef.Mappers;

namespace VpnPortal.Infrastructure.Persistence.Ef.Repositories;

public sealed class EfUserRepository(VpnPortalDbContext dbContext) : IUserRepository
{
    public async Task<IReadOnlyCollection<VpnUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.VpnUsers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(x => x.ToDomain()).ToArray();
    }

    public async Task<VpnUser?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnUser?> GetByIdWithRelationsAsync(int userId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnUsers
            .AsNoTracking()
            .Include(x => x.Devices)
                .ThenInclude(x => x.Credential)
            .Include(x => x.TrustedIps)
            .Include(x => x.Sessions)
                .ThenInclude(x => x.Device)
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var user = entity.ToDomain(includeRelations: true);
        var deviceLookup = user.Devices.ToDictionary(x => x.Id);
        foreach (var session in user.Sessions)
        {
            if (session.DeviceId is int deviceId && deviceLookup.TryGetValue(deviceId, out var device))
            {
                session.Device = device;
            }
        }

        return user;
    }

    public async Task<VpnUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnUser?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken)
    {
        var lowered = login.Trim().ToLowerInvariant();
        var entity = await dbContext.VpnUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email.ToLower() == lowered || x.Username.ToLower() == lowered, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<VpnUser> AddAsync(VpnUser user, CancellationToken cancellationToken)
    {
        var entity = new VpnUserEntity();
        entity.ApplyFromDomain(user);
        await dbContext.VpnUsers.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDomain();
    }

    public async Task UpdateAsync(VpnUser user, CancellationToken cancellationToken)
    {
        var entity = await dbContext.VpnUsers.SingleAsync(x => x.Id == user.Id, cancellationToken);
        entity.ApplyFromDomain(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
