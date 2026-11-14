using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceTier> PriceTiers => Set<PriceTier>();
    public DbSet<CheckoutGroup> CheckoutGroups => Set<CheckoutGroup>();
    public DbSet<DeliveryRun> DeliveryRuns => Set<DeliveryRun>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
