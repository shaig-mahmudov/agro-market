using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Orders;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Admin;

public static class MarkOrderPaid
{
    public sealed record Request(
        PaymentStatus PaymentStatus,
        PaymentMethod? PaymentMethod = null);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PaymentStatus).IsInEnum();
            RuleFor(x => x.PaymentMethod).IsInEnum().When(x => x.PaymentMethod.HasValue);
        }
    }

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
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

        if (order.Status == OrderStatus.Cancelled && request.PaymentStatus == PaymentStatus.Paid)
        {
            return TypedResults.Problem(
                title: "Cannot settle cancelled order",
                detail: "Cancelled orders cannot be marked as Paid.",
                statusCode: StatusCodes.Status409Conflict);
        }

        order.PaymentStatus = request.PaymentStatus;
        if (request.PaymentMethod.HasValue)
        {
            order.PaymentMethod = request.PaymentMethod.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(OrderMappings.ToOrderDto(order));
    }
}
