using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Orders;

public sealed class OrderExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MarketplaceOptions> options,
    ILogger<OrderExpirationWorker> logger) : BackgroundService
{
    private readonly MarketplaceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.ExpirationSweepIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var released = await ReleaseExpiredOrdersAsync(db, stoppingToken);
                if (released > 0)
                {
                    logger.LogInformation("Released stock for {Count} expired pending orders.", released);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while releasing expired pending orders.");
            }
        }
    }

    public static async Task<int> ReleaseExpiredOrdersAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredOrderIds = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt <= now)
            .Select(o => o.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        var releasedCount = 0;

        foreach (var orderId in expiredOrderIds)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var order = await db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order is null || order.Status != OrderStatus.Pending || order.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    await tx.RollbackAsync(cancellationToken);
                    break;
                }

                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                foreach (var item in order.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var product))
                    {
                        product.AvailableStockKg = decimal.Round(product.AvailableStockKg + item.EstimatedWeightKg, 2);
                        product.UpdatedAt = now;
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = now;

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    releasedCount++;
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(cancellationToken);
                }
            }
        }

        return releasedCount;
    }
}
