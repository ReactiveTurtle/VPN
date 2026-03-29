using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnPortal.Infrastructure.Persistence.Ef.Entities;

namespace VpnPortal.Infrastructure.Persistence.Ef.Configurations;

public sealed class SuperAdminConfiguration : IEntityTypeConfiguration<SuperAdminEntity>
{
    public void Configure(EntityTypeBuilder<SuperAdminEntity> builder)
    {
        builder.ToTable("superadmins");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
