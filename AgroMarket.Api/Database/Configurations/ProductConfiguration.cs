using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMarket.Api.Database.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.AvailableStockKg)
            .HasPrecision(12, 2);

        builder.Property(p => p.MinOrderKg)
            .HasPrecision(12, 2);

        builder.Property(p => p.BasePricePerKg)
            .HasPrecision(12, 2);

        builder.OwnsOne(p => p.Metadata, meta =>
        {
            meta.ToJson();
            meta.Property(m => m.Variety).HasMaxLength(120);
            meta.Property(m => m.Grade).HasMaxLength(64);
            meta.Property(m => m.Packaging).HasMaxLength(120);
        });

        builder.Property(p => p.ImageUrls)
            .HasColumnType("text[]");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => new { p.IsDeleted, p.IsActive, p.Category });
        builder.HasIndex(p => p.FarmerId);

        builder.HasOne(p => p.Farmer)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PriceTierConfiguration : IEntityTypeConfiguration<PriceTier>
{
    public void Configure(EntityTypeBuilder<PriceTier> builder)
    {
        builder.ToTable("price_tiers");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.MinQuantityKg)
            .HasPrecision(12, 2);

        builder.Property(pt => pt.PricePerKg)
            .HasPrecision(12, 2);

        builder.HasIndex(pt => new { pt.ProductId, pt.MinQuantityKg })
            .IsUnique();

        builder.HasOne(pt => pt.Product)
            .WithMany(p => p.PriceTiers)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
