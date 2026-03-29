using VpnPortal.Application.Contracts.Requests;
using VpnPortal.Application.Contracts.Account;

namespace VpnPortal.Application.Interfaces;

public interface IRequestService
{
    Task<VpnRequestDto> SubmitAsync(SubmitVpnRequestCommand command, string? remoteIp, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VpnRequestDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<VpnRequestDto?> ApproveAsync(int requestId, string? adminComment, CancellationToken cancellationToken);
    Task<VpnRequestDto?> RejectAsync(int requestId, string? adminComment, CancellationToken cancellationToken);
}
