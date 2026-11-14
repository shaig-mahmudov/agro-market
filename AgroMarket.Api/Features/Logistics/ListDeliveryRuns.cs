using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Logistics;

public static class ListDeliveryRuns
{
    public static async Task<Ok<PagedResult<DeliveryRunDto>>> HandleList(
        AppDbContext db,
        DeliveryRunStatus? status = null,
        DateOnly? runDate = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);
        var query = db.DeliveryRuns.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (runDate.HasValue)
        {
            query = query.Where(r => r.RunDate == runDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var runs = await query
            .OrderByDescending(r => r.RunDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Include(r => r.Orders)
                .ThenInclude(o => o.Items)
            .Include(r => r.Orders)
                .ThenInclude(o => o.CheckoutGroup)
            .Include(r => r.Orders)
                .ThenInclude(o => o.Farmer)
                    .ThenInclude(f => f.BusinessProfile)
            .Include(r => r.Orders)
                .ThenInclude(o => o.Restaurant)
                    .ThenInclude(u => u.BusinessProfile)
            .ToListAsync(cancellationToken);

        var items = runs.Select(LogisticsMappings.ToDto).ToList();
        return TypedResults.Ok(PagedResult<DeliveryRunDto>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Results<Ok<DeliveryRunDto>, ProblemHttpResult>> HandleGetById(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.DeliveryRuns
            .AsNoTracking()
            .Include(r => r.Orders)
                .ThenInclude(o => o.Items)
            .Include(r => r.Orders)
                .ThenInclude(o => o.CheckoutGroup)
            .Include(r => r.Orders)
                .ThenInclude(o => o.Farmer)
                    .ThenInclude(f => f.BusinessProfile)
            .Include(r => r.Orders)
                .ThenInclude(o => o.Restaurant)
                    .ThenInclude(u => u.BusinessProfile)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (run is null)
        {
            return TypedResults.Problem(
                title: "Delivery run not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(LogisticsMappings.ToDto(run));
    }
}
