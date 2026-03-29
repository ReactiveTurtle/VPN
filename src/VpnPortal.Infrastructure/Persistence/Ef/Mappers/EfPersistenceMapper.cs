using System.Net;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Mappers;

internal static class EfPersistenceMapper
{
    public static VpnUser ToDomain(this VpnUserEntity entity, bool includeRelations = false)
    {
        return new VpnUser
        {
            Id = checked((int)entity.Id),
            Email = entity.Email,
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            MaxDevices = entity.MaxDevices,
            Active = entity.Active,
            EmailConfirmed = entity.EmailConfirmed,
            CreatedAt = entity.CreatedAt,
            LastLoginAt = entity.LastLoginAt,
            Devices = includeRelations ? entity.Devices.Select(x => x.ToDomain()).ToList() : [],
            TrustedIps = includeRelations ? entity.TrustedIps.Select(x => x.ToDomain()).ToList() : [],
            Sessions = includeRelations ? entity.Sessions.Select(x => x.ToDomain()).ToList() : []
        };
    }

    public static void ApplyFromDomain(this VpnUserEntity entity, VpnUser domain)
    {
        entity.Email = domain.Email;
        entity.Username = domain.Username;
        entity.PasswordHash = domain.PasswordHash;
        entity.MaxDevices = domain.MaxDevices;
        entity.Active = domain.Active;
        entity.EmailConfirmed = domain.EmailConfirmed;
        entity.CreatedAt = domain.CreatedAt;
        entity.LastLoginAt = domain.LastLoginAt;
        entity.DeactivatedAt = domain.Active ? null : entity.DeactivatedAt ?? DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static TrustedDevice ToDomain(this TrustedDeviceEntity entity)
    {
        return new TrustedDevice
        {
            Id = checked((int)entity.Id),
            UserId = checked((int)entity.UserId),
            DeviceUuid = entity.DeviceUuid,
            DeviceName = entity.DeviceName ?? "Unnamed device",
            DeviceType = entity.DeviceType,
            Platform = entity.Platform,
            Status = ParseDeviceStatus(entity.Status),
            FirstSeenAt = entity.FirstSeenAt,
            LastSeenAt = entity.LastSeenAt,
            ActiveCredential = entity.Credential?.ToDomain()
        };
    }

    public static void ApplyFromDomain(this TrustedDeviceEntity entity, TrustedDevice domain)
    {
        entity.UserId = domain.UserId;
        entity.DeviceUuid = domain.DeviceUuid;
        entity.DeviceName = domain.DeviceName;
        entity.DeviceType = domain.DeviceType;
        entity.Platform = domain.Platform;
        entity.Status = ToStorage(domain.Status);
        entity.FirstSeenAt = domain.FirstSeenAt;
        entity.LastSeenAt = domain.LastSeenAt;
        entity.ApprovedAt = domain.Status == DeviceStatus.Active ? domain.FirstSeenAt : entity.ApprovedAt;
        entity.RevokedAt = domain.Status == DeviceStatus.Revoked ? DateTimeOffset.UtcNow : entity.RevokedAt;
    }

    public static VpnDeviceCredential ToDomain(this VpnDeviceCredentialEntity entity)
    {
        return new VpnDeviceCredential
        {
            Id = checked((int)entity.Id),
            UserId = checked((int)entity.UserId),
            DeviceId = checked((int)entity.DeviceId),
            VpnUsername = entity.VpnUsername,
            PasswordHash = entity.PasswordHash,
            RadiusNtHash = entity.RadiusNtHash,
            Status = ParseCredentialStatus(entity.Status),
            CreatedAt = entity.CreatedAt,
            RotatedAt = entity.RotatedAt,
            RevokedAt = entity.RevokedAt,
            LastUsedAt = entity.LastUsedAt
        };
    }

    public static void ApplyFromDomain(this VpnDeviceCredentialEntity entity, VpnDeviceCredential domain)
    {
        entity.UserId = domain.UserId;
        entity.DeviceId = domain.DeviceId;
        entity.VpnUsername = domain.VpnUsername;
        entity.PasswordHash = domain.PasswordHash;
        entity.RadiusNtHash = domain.RadiusNtHash;
        entity.Status = ToStorage(domain.Status);
        entity.CreatedAt = domain.CreatedAt;
        entity.RotatedAt = domain.RotatedAt;
        entity.RevokedAt = domain.RevokedAt;
        entity.LastUsedAt = domain.LastUsedAt;
    }

    public static TrustedIp ToDomain(this TrustedIpEntity entity)
    {
        return new TrustedIp
        {
            Id = checked((int)entity.Id),
            UserId = checked((int)entity.UserId),
            DeviceId = entity.DeviceId is null ? null : checked((int)entity.DeviceId.Value),
            IpAddress = entity.IpAddress.ToString(),
            Status = ParseTrustedIpStatus(entity.Status),
            FirstSeenAt = entity.FirstSeenAt,
            LastSeenAt = entity.LastSeenAt,
            ApprovedAt = entity.ApprovedAt,
            RevokedAt = entity.RevokedAt
        };
    }

    public static void ApplyFromDomain(this TrustedIpEntity entity, TrustedIp domain)
    {
        entity.UserId = domain.UserId;
        entity.DeviceId = domain.DeviceId;
        entity.IpAddress = ParseRequiredIpAddress(domain.IpAddress);
        entity.Status = ToStorage(domain.Status);
        entity.FirstSeenAt = domain.FirstSeenAt;
        entity.LastSeenAt = domain.LastSeenAt;
        entity.ApprovedAt = domain.ApprovedAt;
        entity.RevokedAt = domain.RevokedAt;
    }

    public static VpnSession ToDomain(this VpnSessionEntity entity)
    {
        return new VpnSession
        {
            Id = checked((int)entity.Id),
            UserId = checked((int)entity.UserId),
            DeviceId = entity.DeviceId is null ? null : checked((int)entity.DeviceId.Value),
            SourceIp = entity.SourceIp.ToString(),
            AssignedVpnIp = entity.AssignedVpnIp?.ToString(),
            NasIdentifier = entity.NasIdentifier,
            SessionId = entity.SessionId,
            StartedAt = entity.StartedAt,
            LastSeenAt = entity.LastSeenAt,
            EndedAt = entity.EndedAt,
            TerminationReason = entity.TerminationReason,
            Active = entity.Active,
            Authorized = entity.Authorized,
            Device = entity.Device is null ? null : new TrustedDevice { Id = checked((int)entity.Device.Id), DeviceName = entity.Device.DeviceName ?? "Unknown device" },
            User = entity.User is null ? null : new VpnUser { Id = checked((int)entity.User.Id), Username = entity.User.Username }
        };
    }

    public static void ApplyFromDomain(this VpnSessionEntity entity, VpnSession domain)
    {
        entity.UserId = domain.UserId;
        entity.DeviceId = domain.DeviceId;
        entity.SourceIp = ParseRequiredIpAddress(domain.SourceIp);
        entity.AssignedVpnIp = ParseIpAddress(domain.AssignedVpnIp);
        entity.NasIdentifier = domain.NasIdentifier;
        entity.SessionId = domain.SessionId ?? string.Empty;
        entity.StartedAt = domain.StartedAt;
        entity.LastSeenAt = domain.LastSeenAt;
        entity.EndedAt = domain.EndedAt;
        entity.TerminationReason = domain.TerminationReason;
        entity.Active = domain.Active;
        entity.Authorized = domain.Authorized;
    }

    public static VpnRequest ToDomain(this VpnRequestEntity entity)
    {
        return new VpnRequest
        {
            Id = checked((int)entity.Id),
            Email = entity.Email,
            Name = entity.Name,
            RequestedByIp = entity.RequestedByIp?.ToString(),
            Status = ParseRequestStatus(entity.Status),
            AdminComment = entity.AdminComment,
            SubmittedAt = entity.SubmittedAt,
            ProcessedAt = entity.ProcessedAt
        };
    }

    public static void ApplyFromDomain(this VpnRequestEntity entity, VpnRequest domain)
    {
        entity.Email = domain.Email;
        entity.Name = domain.Name;
        entity.RequestedByIp = ParseIpAddress(domain.RequestedByIp);
        entity.Status = ToStorage(domain.Status);
        entity.AdminComment = domain.AdminComment;
        entity.SubmittedAt = domain.SubmittedAt;
        entity.ProcessedAt = domain.ProcessedAt;
    }

    public static AccountToken ToDomain(this AccountTokenEntity entity)
    {
        return new AccountToken
        {
            Id = checked((int)entity.Id),
            UserEmail = entity.UserEmail,
            TokenHash = entity.TokenHash,
            Purpose = ParsePurpose(entity.Purpose),
            ExpiresAt = entity.ExpiresAt,
            Used = entity.Used,
            UsedAt = entity.UsedAt,
            CreatedAt = entity.CreatedAt
        };
    }

    public static void ApplyFromDomain(this AccountTokenEntity entity, AccountToken domain)
    {
        entity.UserEmail = domain.UserEmail;
        entity.TokenHash = domain.TokenHash;
        entity.Purpose = ToStorage(domain.Purpose);
        entity.ExpiresAt = domain.ExpiresAt;
        entity.Used = domain.Used;
        entity.UsedAt = domain.UsedAt;
        entity.CreatedAt = domain.CreatedAt;
    }

    public static SuperAdmin ToDomain(this SuperAdminEntity entity)
    {
        return new SuperAdmin
        {
            Id = checked((int)entity.Id),
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            CreatedAt = entity.CreatedAt,
            LastLoginAt = entity.LastLoginAt
        };
    }

    public static void ApplyFromDomain(this SuperAdminEntity entity, SuperAdmin domain)
    {
        entity.Username = domain.Username;
        entity.PasswordHash = domain.PasswordHash;
        entity.CreatedAt = domain.CreatedAt;
        entity.LastLoginAt = domain.LastLoginAt;
    }

    public static IpChangeConfirmation ToDomain(this IpChangeConfirmationEntity entity)
    {
        return new IpChangeConfirmation
        {
            Id = checked((int)entity.Id),
            UserId = checked((int)entity.UserId),
            DeviceId = entity.DeviceId is null ? null : checked((int)entity.DeviceId.Value),
            RequestedIp = entity.RequestedIp.ToString(),
            TokenHash = entity.TokenHash,
            Status = ParseIpConfirmationStatus(entity.Status),
            ExpiresAt = entity.ExpiresAt,
            CreatedAt = entity.CreatedAt,
            ConfirmedAt = entity.ConfirmedAt
        };
    }

    public static void ApplyFromDomain(this IpChangeConfirmationEntity entity, IpChangeConfirmation domain)
    {
        entity.UserId = domain.UserId;
        entity.DeviceId = domain.DeviceId;
        entity.RequestedIp = ParseRequiredIpAddress(domain.RequestedIp);
        entity.TokenHash = domain.TokenHash;
        entity.Status = ToStorage(domain.Status);
        entity.ExpiresAt = domain.ExpiresAt;
        entity.CreatedAt = domain.CreatedAt;
        entity.ConfirmedAt = domain.ConfirmedAt;
    }

    public static AuditLogEntry ToDomain(this AuditLogEntity entity)
    {
        return new AuditLogEntry
        {
            Id = entity.Id,
            ActorType = entity.ActorType,
            ActorId = entity.ActorId,
            Action = entity.Action,
            EntityType = entity.EntityType,
            EntityId = entity.EntityId,
            IpAddress = entity.IpAddress?.ToString(),
            DetailsJson = entity.DetailsJson,
            CreatedAt = entity.CreatedAt
        };
    }

    public static void ApplyFromDomain(this AuditLogEntity entity, AuditLogEntry domain)
    {
        entity.ActorType = domain.ActorType;
        entity.ActorId = domain.ActorId;
        entity.Action = domain.Action;
        entity.EntityType = domain.EntityType;
        entity.EntityId = domain.EntityId;
        entity.IpAddress = ParseIpAddress(domain.IpAddress);
        entity.DetailsJson = domain.DetailsJson;
        entity.CreatedAt = domain.CreatedAt;
    }

    private static string ToStorage(DeviceStatus status) => status.ToString().ToLowerInvariant();
    private static string ToStorage(VpnDeviceCredentialStatus status) => status.ToString().ToLowerInvariant();
    private static string ToStorage(TrustedIpStatus status) => status.ToString().ToLowerInvariant();
    private static string ToStorage(IpChangeConfirmationStatus status) => status.ToString().ToLowerInvariant();
    private static string ToStorage(RequestStatus status) => status.ToString().ToLowerInvariant();
    private static string ToStorage(AccountTokenPurpose purpose) => purpose switch
    {
        AccountTokenPurpose.AccountActivation => "account_activation",
        AccountTokenPurpose.IpConfirmation => "ip_confirmation",
        AccountTokenPurpose.PasswordReset => "password_reset",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
    };

    private static DeviceStatus ParseDeviceStatus(string value) => Enum.Parse<DeviceStatus>(value, true);
    private static VpnDeviceCredentialStatus ParseCredentialStatus(string value) => Enum.Parse<VpnDeviceCredentialStatus>(value, true);
    private static TrustedIpStatus ParseTrustedIpStatus(string value) => Enum.Parse<TrustedIpStatus>(value, true);
    private static IpChangeConfirmationStatus ParseIpConfirmationStatus(string value) => Enum.Parse<IpChangeConfirmationStatus>(value, true);
    private static RequestStatus ParseRequestStatus(string value) => Enum.Parse<RequestStatus>(value, true);
    private static AccountTokenPurpose ParsePurpose(string value) => value switch
    {
        "account_activation" => AccountTokenPurpose.AccountActivation,
        "ip_confirmation" => AccountTokenPurpose.IpConfirmation,
        "password_reset" => AccountTokenPurpose.PasswordReset,
        _ => throw new InvalidOperationException($"Unsupported token purpose '{value}'.")
    };

    private static IPAddress? ParseIpAddress(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : IPAddress.Parse(value);
    }

    private static IPAddress ParseRequiredIpAddress(string value)
    {
        return IPAddress.Parse(value);
    }
}
