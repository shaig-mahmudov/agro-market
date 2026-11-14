using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMarket.Api.Database.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique();

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(u => u.TelegramChatId)
            .IsUnique()
            .HasFilter("\"TelegramChatId\" IS NOT NULL");

        builder.Property(u => u.IsVerified)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique();

        builder.HasIndex(rt => rt.UserId);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
{
    public void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.ToTable("business_profiles");

        builder.HasKey(bp => bp.UserId);

        builder.Property(bp => bp.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bp => bp.Region)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(bp => bp.Address)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(bp => bp.Latitude)
            .HasPrecision(9, 6);

        builder.Property(bp => bp.Longitude)
            .HasPrecision(9, 6);

        builder.Property(bp => bp.PreferredDeliveryWindow)
            .HasMaxLength(64);

        builder.Property(bp => bp.TaxId)
            .HasMaxLength(64);

        builder.HasOne(bp => bp.User)
            .WithOne(u => u.BusinessProfile)
            .HasForeignKey<BusinessProfile>(bp => bp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
