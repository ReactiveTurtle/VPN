using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryRequestRepository(InMemoryPortalStore store) : IRequestRepository
{
    public Task<VpnRequest?> GetLatestPendingByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var request = store.Requests
            .Where(x => x.Email == email && x.Status == RequestStatus.Pending)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(Clone)
            .FirstOrDefault();

        return Task.FromResult(request);
    }

    public Task<VpnRequest> AddAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        var copy = Clone(request);
        copy.Id = store.AllocateRequestId();
        store.Requests.Add(copy);
        return Task.FromResult(Clone(copy));
    }

    public Task<IReadOnlyCollection<VpnRequest>> GetAllAsync(CancellationToken cancellationToken)
    {
        var requests = store.Requests
            .OrderByDescending(x => x.SubmittedAt)
            .Select(Clone)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<VpnRequest>>(requests);
    }

    public Task<VpnRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = store.Requests.Where(x => x.Id == requestId).Select(Clone).FirstOrDefault();
        return Task.FromResult(request);
    }

    public Task UpdateAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        var current = store.Requests.First(x => x.Id == request.Id);
        current.Email = request.Email;
        current.Name = request.Name;
        current.RequestedByIp = request.RequestedByIp;
        current.Status = request.Status;
        current.AdminComment = request.AdminComment;
        current.SubmittedAt = request.SubmittedAt;
        current.ProcessedAt = request.ProcessedAt;
        return Task.CompletedTask;
    }

    private static VpnRequest Clone(VpnRequest source)
    {
        return new VpnRequest
        {
            Id = source.Id,
            Email = source.Email,
            Name = source.Name,
            RequestedByIp = source.RequestedByIp,
            Status = source.Status,
            AdminComment = source.AdminComment,
            SubmittedAt = source.SubmittedAt,
            ProcessedAt = source.ProcessedAt
        };
    }
}
