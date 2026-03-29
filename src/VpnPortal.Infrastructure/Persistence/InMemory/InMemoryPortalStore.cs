using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPortalStore
{
    private readonly object sync = new();
    private int nextRequestId = 3;
    private int nextUserId = 2;
    private int nextDeviceId = 2;
    private int nextDeviceCredentialId = 2;
    private int nextSessionId = 2;
    private int nextTokenId = 2;
    private int nextTrustedIpId = 2;
    private int nextIpConfirmationId = 2;

    public List<VpnUser> Users { get; } =
    [
        new VpnUser
        {
            Id = 1,
            Email = "alex@example.com",
            Username = "alex",
            PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$rxaqCBytGMHXfA9JHW0Dug==$8jk57FB8d7rL95gz8krS8Zr0+hI4s/wPzblAvNlIV1A=",
            Active = true,
            EmailConfirmed = true,
            MaxDevices = 2,
            CreatedAt = new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero),
            Devices =
            [
                new TrustedDevice
                {
                    Id = 1,
                    UserId = 1,
                    DeviceUuid = "ios-alex-001",
                    DeviceName = "Alex iPhone",
                    DeviceType = "phone",
                    Platform = "ios",
                    Status = DeviceStatus.Active,
                    FirstSeenAt = new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero),
                    LastSeenAt = new DateTimeOffset(2026, 3, 27, 8, 30, 0, TimeSpan.Zero)
                }
            ],
            Sessions =
            [
                new VpnSession
                {
                    Id = 1,
                    UserId = 1,
                    DeviceId = 1,
                    SourceIp = "203.0.113.50",
                    AssignedVpnIp = "10.10.0.12",
                    SessionId = "seed-session-001",
                    StartedAt = new DateTimeOffset(2026, 3, 27, 8, 0, 0, TimeSpan.Zero),
                    LastSeenAt = new DateTimeOffset(2026, 3, 27, 8, 35, 0, 0, TimeSpan.Zero),
                    Active = true,
                    Authorized = true
                }
            ],
            TrustedIps =
            [
                new TrustedIp
                {
                    Id = 1,
                    UserId = 1,
                    DeviceId = 1,
                    IpAddress = "203.0.113.50",
                    Status = TrustedIpStatus.Active,
                    FirstSeenAt = new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero),
                    LastSeenAt = new DateTimeOffset(2026, 3, 27, 8, 35, 0, TimeSpan.Zero),
                    ApprovedAt = new DateTimeOffset(2026, 3, 26, 12, 5, 0, TimeSpan.Zero)
                }
            ]
        }
    ];

    public List<VpnRequest> Requests { get; } =
    [
        new VpnRequest
        {
            Id = 1,
            Email = "pending.user@example.com",
            Name = "Pending User",
            RequestedByIp = "203.0.113.24",
            Status = RequestStatus.Pending,
            SubmittedAt = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero)
        },
        new VpnRequest
        {
            Id = 2,
            Email = "approved.user@example.com",
            Name = "Approved User",
            RequestedByIp = "198.51.100.10",
            Status = RequestStatus.Approved,
            AdminComment = "Approved for internal testing",
            SubmittedAt = new DateTimeOffset(2026, 3, 24, 9, 30, 0, TimeSpan.Zero),
            ProcessedAt = new DateTimeOffset(2026, 3, 24, 10, 0, 0, TimeSpan.Zero)
        }
    ];

    public List<SuperAdmin> SuperAdmins { get; } =
    [
        new SuperAdmin
        {
            Id = 1,
            Username = "rootadmin",
            PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$rxaqCBytGMHXfA9JHW0Dug==$8jk57FB8d7rL95gz8krS8Zr0+hI4s/wPzblAvNlIV1A=",
            CreatedAt = new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero)
        }
    ];

    public List<AccountToken> AccountTokens { get; } =
    [
        new AccountToken
        {
            Id = 1,
            UserEmail = "approved.user@example.com",
            TokenHash = "7c43ef5ae21d43ce2743f770c68e24def1a43ee2f416d2438410c8af7af2ff2c",
            Purpose = AccountTokenPurpose.AccountActivation,
            ExpiresAt = new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero),
            Used = false,
            CreatedAt = new DateTimeOffset(2026, 3, 27, 10, 0, 0, TimeSpan.Zero)
        }
    ];

    public List<IpChangeConfirmation> IpChangeConfirmations { get; } =
    [
        new IpChangeConfirmation
        {
            Id = 1,
            UserId = 1,
            DeviceId = 1,
            RequestedIp = "198.51.100.77",
            TokenHash = "pending-ip-demo-hash",
            Status = IpChangeConfirmationStatus.Pending,
            ExpiresAt = new DateTimeOffset(2026, 3, 28, 12, 0, 0, TimeSpan.Zero),
            CreatedAt = new DateTimeOffset(2026, 3, 27, 12, 0, 0, TimeSpan.Zero)
        }
    ];

    public List<VpnDeviceCredential> DeviceCredentials { get; } =
    [
        new VpnDeviceCredential
        {
            Id = 1,
            UserId = 1,
            DeviceId = 1,
            VpnUsername = "alex.d1",
            PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$rxaqCBytGMHXfA9JHW0Dug==$8jk57FB8d7rL95gz8krS8Zr0+hI4s/wPzblAvNlIV1A=",
            RadiusNtHash = "2B576ACBE6BCFDA7294D6BD18041B8FE",
            Status = VpnDeviceCredentialStatus.Active,
            CreatedAt = new DateTimeOffset(2026, 3, 27, 8, 0, 0, TimeSpan.Zero)
        }
    ];

    public int AllocateRequestId()
    {
        lock (sync)
        {
            return nextRequestId++;
        }
    }

    public int AllocateUserId()
    {
        lock (sync)
        {
            return nextUserId++;
        }
    }

    public int AllocateDeviceId()
    {
        lock (sync)
        {
            return nextDeviceId++;
        }
    }

    public int AllocateDeviceCredentialId()
    {
        lock (sync)
        {
            return nextDeviceCredentialId++;
        }
    }

    public int AllocateSessionId()
    {
        lock (sync)
        {
            return nextSessionId++;
        }
    }

    public int AllocateTokenId()
    {
        lock (sync)
        {
            return nextTokenId++;
        }
    }

    public int AllocateTrustedIpId()
    {
        lock (sync)
        {
            return nextTrustedIpId++;
        }
    }

    public int AllocateIpConfirmationId()
    {
        lock (sync)
        {
            return nextIpConfirmationId++;
        }
    }
}
