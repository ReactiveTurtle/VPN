using Microsoft.EntityFrameworkCore;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence;

public sealed class PortalDbContext(DbContextOptions<PortalDbContext> options) : DbContext(options)
{
    public DbSet<VpnUser> Users => Set<VpnUser>();
    public DbSet<VpnRequest> Requests => Set<VpnRequest>();
    public DbSet<TrustedDevice> Devices => Set<TrustedDevice>();
    public DbSet<VpnSession> Sessions => Set<VpnSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VpnUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Username).HasMaxLength(64);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<VpnRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<TrustedDevice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.DeviceUuid }).IsUnique();
            entity.Property(x => x.DeviceUuid).HasMaxLength(128);
            entity.Property(x => x.DeviceName).HasMaxLength(255);
            entity.Property(x => x.DeviceType).HasMaxLength(32);
            entity.Property(x => x.Platform).HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<VpnSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceIp).HasMaxLength(64);
            entity.Property(x => x.AssignedVpnIp).HasMaxLength(64);
            entity.Property(x => x.SessionId).HasMaxLength(128);
        });

        modelBuilder.Entity<VpnUser>().HasData(new VpnUser
        {
            Id = 1,
            Email = "alex@example.com",
            Username = "alex",
            PasswordHash = "$argon2id$seeded-demo-only",
            Active = true,
            EmailConfirmed = true,
            MaxDevices = 2,
            CreatedAt = new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero)
        });

        modelBuilder.Entity<VpnRequest>().HasData(
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
            });

        modelBuilder.Entity<TrustedDevice>().HasData(new TrustedDevice
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
        });

        modelBuilder.Entity<VpnSession>().HasData(new VpnSession
        {
            Id = 1,
            UserId = 1,
            DeviceId = 1,
            SourceIp = "203.0.113.50",
            AssignedVpnIp = "10.10.0.12",
            SessionId = "seed-session-001",
            StartedAt = new DateTimeOffset(2026, 3, 27, 8, 0, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 3, 27, 8, 35, 0, TimeSpan.Zero),
            Active = true,
            Authorized = true
        });
    }
}
