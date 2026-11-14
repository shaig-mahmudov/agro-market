using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Orders;

public static class ListOrders
{
    public static async Task<Ok<PagedResult<CheckoutGroupDto>>> HandleListCheckoutGroups(
        ClaimsPrincipal principal,
        AppDbContext db,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);

        var query = db.CheckoutGroups.AsNoTracking();
        if (role != UserRole.Admin)
        {
            query = query.Where(cg => cg.RestaurantId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var groups = await query
            .OrderByDescending(cg => cg.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Items)
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Farmer)
                    .ThenInclude(f => f.BusinessProfile)
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Restaurant)
                    .ThenInclude(r => r.BusinessProfile)
            .ToListAsync(cancellationToken);

        var items = groups.Select(OrderMappings.ToCheckoutGroupDto).ToList();
        return TypedResults.Ok(PagedResult<CheckoutGroupDto>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Results<Ok<CheckoutGroupDto>, ProblemHttpResult>> HandleGetCheckoutGroup(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var group = await db.CheckoutGroups
            .AsNoTracking()
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Items)
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Farmer)
                    .ThenInclude(f => f.BusinessProfile)
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Restaurant)
                    .ThenInclude(r => r.BusinessProfile)
            .FirstOrDefaultAsync(cg => cg.Id == id, cancellationToken);

        if (group is null)
        {
            return TypedResults.Problem(title: "Checkout group not found", statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && group.RestaurantId != userId)
        {
            return TypedResults.Problem(title: "Forbidden", statusCode: StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(OrderMappings.ToCheckoutGroupDto(group));
    }

    public static async Task<Ok<PagedResult<OrderDto>>> HandleListOrders(
        ClaimsPrincipal principal,
        AppDbContext db,
        OrderStatus? status = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);

        var query = db.Orders.AsNoTracking();

        query = role switch
        {
            UserRole.Admin => query,
            UserRole.Farmer => query.Where(o => o.FarmerId == userId),
            UserRole.Restaurant => query.Where(o => o.RestaurantId == userId),
            _ => query.Where(_ => false)
        };

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Include(o => o.Items)
            .Include(o => o.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.BusinessProfile)
            .ToListAsync(cancellationToken);

        var items = orders.Select(OrderMappings.ToOrderDto).ToList();
        return TypedResults.Ok(PagedResult<OrderDto>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> HandleGetOrder(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.BusinessProfile)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            return TypedResults.Problem(title: "Order not found", statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && order.RestaurantId != userId && order.FarmerId != userId)
        {
            return TypedResults.Problem(title: "Forbidden", statusCode: StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(OrderMappings.ToOrderDto(order));
    }
}
