using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Orders;

public static class ConfirmReceipt
{
    public sealed record Request(
        bool Approve,
        string? DisputeNote = null);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DisputeNote).MaximumLength(500);
        }
    }

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.BusinessProfile)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            return TypedResults.Problem(
                title: "Order not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && order.RestaurantId != userId)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Only the ordering restaurant or an admin can confirm or dispute delivery receipt.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (order.Status is not (OrderStatus.DeliveredPendingApproval or OrderStatus.Disputed))
        {
            return TypedResults.Problem(
                title: "Order does not await buyer approval",
                detail: $"Order is currently {order.Status}; only DeliveredPendingApproval or Disputed orders can be confirmed or disputed.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (request.Approve)
        {
            order.Status = OrderStatus.Delivered;
            order.DeliveredAt ??= DateTimeOffset.UtcNow;
            if (order.PaymentStatus == PaymentStatus.Disputed)
            {
                order.PaymentStatus = PaymentStatus.Unpaid;
            }
        }
        else
        {
            order.Status = OrderStatus.Disputed;
            order.DeliveredAt = null;
            order.PaymentStatus = PaymentStatus.Disputed;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(OrderMappings.ToOrderDto(order));
    }
}
