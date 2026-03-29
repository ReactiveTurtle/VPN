using VpnPortal.Application.Contracts.Requests;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Enums;
using VpnPortal.Domain.Entities;

namespace VpnPortal.Infrastructure.Services;

public sealed class RequestService(
    IRequestRepository requestRepository,
    IUserRepository userRepository,
    IAccountTokenRepository accountTokenRepository,
    ITokenProtector tokenProtector,
    IEmailService emailService,
    IAuditService auditService) : IRequestService
{
    public async Task<VpnRequestDto> SubmitAsync(SubmitVpnRequestCommand command, string? remoteIp, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var existingPending = await requestRepository.GetLatestPendingByEmailAsync(email, cancellationToken);

        if (existingPending is not null)
        {
            return Map(existingPending);
        }

        var entity = new VpnRequest
        {
            Email = email,
            Name = string.IsNullOrWhiteSpace(command.Name) ? null : command.Name.Trim(),
            RequestedByIp = remoteIp,
            Status = RequestStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        entity = await requestRepository.AddAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyCollection<VpnRequestDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var requests = await requestRepository.GetAllAsync(cancellationToken);
        return requests.Select(Map).ToArray();
    }

    public async Task<VpnRequestDto?> ApproveAsync(int requestId, string? adminComment, CancellationToken cancellationToken)
    {
        var entity = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = RequestStatus.Approved;
        entity.AdminComment = string.IsNullOrWhiteSpace(adminComment) ? "Approved" : adminComment.Trim();
        entity.ProcessedAt = DateTimeOffset.UtcNow;
        await requestRepository.UpdateAsync(entity, cancellationToken);

        var user = await userRepository.GetByEmailAsync(entity.Email, cancellationToken);
        if (user is null)
        {
            user = new VpnUser
            {
                Email = entity.Email,
                Username = BuildUsername(entity.Email),
                PasswordHash = string.Empty,
                Active = true,
                EmailConfirmed = false,
                CreatedAt = DateTimeOffset.UtcNow,
                MaxDevices = 2
            };

            user = await userRepository.AddAsync(user, cancellationToken);
        }

        var rawToken = tokenProtector.GenerateRawToken();
        var accountToken = new AccountToken
        {
            UserEmail = entity.Email,
            TokenHash = tokenProtector.Hash(rawToken),
            Purpose = AccountTokenPurpose.AccountActivation,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Used = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        accountToken = await accountTokenRepository.AddAsync(accountToken, cancellationToken);
        var activationLink = $"/activate/{rawToken}";
        await emailService.SendActivationLinkAsync(entity.Email, activationLink, accountToken.ExpiresAt, cancellationToken);
        await auditService.WriteAsync("superadmin", null, "request_approved", "vpn_request", entity.Id.ToString(), null, new { entity.Email, activationLink }, cancellationToken);
        return Map(entity);

        VpnRequestDto Map(VpnRequest request)
        {
            return new VpnRequestDto(
                request.Id,
                request.Email,
                request.Name,
                request.Status.ToString().ToLowerInvariant(),
                request.AdminComment,
                request.SubmittedAt,
                request.ProcessedAt,
                accountToken.ExpiresAt,
                activationLink);
        }
    }

    public async Task<VpnRequestDto?> RejectAsync(int requestId, string? adminComment, CancellationToken cancellationToken)
    {
        var entity = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = RequestStatus.Rejected;
        entity.AdminComment = string.IsNullOrWhiteSpace(adminComment) ? "Rejected" : adminComment.Trim();
        entity.ProcessedAt = DateTimeOffset.UtcNow;
        await requestRepository.UpdateAsync(entity, cancellationToken);
        await auditService.WriteAsync("superadmin", null, "request_rejected", "vpn_request", entity.Id.ToString(), null, new { entity.Email }, cancellationToken);
        return Map(entity);
    }

    private static VpnRequestDto Map(VpnRequest entity)
    {
        return new VpnRequestDto(
            entity.Id,
            entity.Email,
            entity.Name,
            entity.Status.ToString().ToLowerInvariant(),
            entity.AdminComment,
            entity.SubmittedAt,
            entity.ProcessedAt,
            null,
            null);
    }

    private static string BuildUsername(string email)
    {
        var baseName = email.Split('@')[0].Trim().ToLowerInvariant();
        var safe = new string(baseName.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? $"user{Guid.NewGuid():N}"[..12] : safe;
    }
}
