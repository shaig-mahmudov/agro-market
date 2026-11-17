using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMarket.Api.Database.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Folder)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ContentType)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(m => m.Data)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasIndex(m => m.OwnerId);
        builder.HasIndex(m => new { m.Folder, m.CreatedAt });

        builder.HasOne(m => m.Owner)
            .WithMany()
            .HasForeignKey(m => m.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
