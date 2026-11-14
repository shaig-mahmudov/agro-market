using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Orders;

public static class FarmerRespond
{
    public sealed record Request(bool Accept);

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var isAdmin = principal.GetUserRole() == UserRole.Admin;

        return await RespondToOrderAsync(id, userId, isAdmin, request.Accept, db, cancellationToken);
    }

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> RespondToOrderAsync(
        Guid orderId,
        Guid actorUserId,
        bool isAdmin,
        bool accept,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.BusinessProfile)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return TypedResults.Problem(
                title: "Order not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!isAdmin && order.FarmerId != actorUserId)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Only the assigned farmer can respond to this order.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (order.Status != OrderStatus.Pending)
        {
            return TypedResults.Problem(
                title: "Invalid order state",
                detail: $"Order is already '{order.Status}' and cannot be modified.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;
        if (order.ExpiresAt <= now)
        {
            return TypedResults.Problem(
                title: "Order expired",
                detail: "The confirmation window for this order has expired.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (accept)
        {
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = now;
        }
        else
        {
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
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(cancellationToken);
            return TypedResults.Problem(
                title: "Concurrency conflict",
                detail: "Stock changed concurrently while updating this order. Please retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(OrderMappings.ToOrderDto(order));
    }
}
