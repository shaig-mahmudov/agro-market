using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMarket.Api.Database.Configurations;

public sealed class CheckoutGroupConfiguration : IEntityTypeConfiguration<CheckoutGroup>
{
    public void Configure(EntityTypeBuilder<CheckoutGroup> builder)
    {
        builder.ToTable("checkout_groups");

        builder.HasKey(cg => cg.Id);

        builder.Property(cg => cg.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(cg => cg.TotalDeliveryFee)
            .HasPrecision(12, 2);

        builder.HasIndex(cg => new { cg.RestaurantId, cg.CreatedAt });

        builder.HasOne(cg => cg.Restaurant)
            .WithMany()
            .HasForeignKey(cg => cg.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.FulfillmentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(o => o.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.EstimatedTotalAmount).HasPrecision(12, 2);
        builder.Property(o => o.PlatformFeeAmount).HasPrecision(12, 2);
        builder.Property(o => o.DeliveryFeeAmount).HasPrecision(12, 2);
        builder.Property(o => o.FinalTotalAmount).HasPrecision(12, 2);

        builder.HasIndex(o => new { o.FarmerId, o.Status });
        builder.HasIndex(o => new { o.RestaurantId, o.CreatedAt });
        builder.HasIndex(o => new { o.Status, o.ExpiresAt });
        builder.HasIndex(o => o.DeliveryRunId);

        builder.HasOne(o => o.CheckoutGroup)
            .WithMany(cg => cg.Orders)
            .HasForeignKey(o => o.CheckoutGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Restaurant)
            .WithMany()
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Farmer)
            .WithMany()
            .HasForeignKey(o => o.FarmerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.DeliveryRun)
            .WithMany(dr => dr.Orders)
            .HasForeignKey(o => o.DeliveryRunId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductTitleSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.UnitPriceApplied).HasPrecision(12, 2);
        builder.Property(oi => oi.EstimatedWeightKg).HasPrecision(12, 2);
        builder.Property(oi => oi.ActualDeliveredWeightKg).HasPrecision(12, 2);
        builder.Property(oi => oi.RejectedWeightKg).HasPrecision(12, 2);

        builder.Property(oi => oi.ProofPhotoUrl)
            .HasMaxLength(1000);

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
