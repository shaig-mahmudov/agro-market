using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMarket.Api.Database.Configurations;

public sealed class DeliveryRunConfiguration : IEntityTypeConfiguration<DeliveryRun>
{
    public void Configure(EntityTypeBuilder<DeliveryRun> builder)
    {
        builder.ToTable("delivery_runs");

        builder.HasKey(dr => dr.Id);

        builder.Property(dr => dr.DriverName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(dr => dr.DriverPhone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(dr => dr.VehicleNote)
            .HasMaxLength(300);

        builder.Property(dr => dr.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(dr => new { dr.RunDate, dr.Status });
    }
}
