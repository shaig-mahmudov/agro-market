using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Logistics;

public static class UpdateRunStatus
{
    public sealed record Request(DeliveryRunStatus Status);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Status).IsInEnum();
        }
    }

    public static async Task<Results<Ok<DeliveryRunDto>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.DeliveryRuns
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

        if (run.Status == request.Status)
        {
            return TypedResults.Ok(LogisticsMappings.ToDto(run));
        }

        var isValidTransition = (run.Status, request.Status) switch
        {
            (DeliveryRunStatus.Planned, DeliveryRunStatus.PickingUp) => true,
            (DeliveryRunStatus.PickingUp, DeliveryRunStatus.InTransit) => true,
            (DeliveryRunStatus.InTransit, DeliveryRunStatus.Completed) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            return TypedResults.Problem(
                title: "Invalid run status transition",
                detail: $"Cannot transition delivery run from {run.Status} to {request.Status}. Expected order: Planned -> PickingUp -> InTransit -> Completed.",
                statusCode: StatusCodes.Status409Conflict);
        }

        run.Status = request.Status;

        if (request.Status == DeliveryRunStatus.PickingUp)
        {
            foreach (var order in run.Orders.Where(o => o.Status == OrderStatus.Confirmed))
            {
                order.Status = OrderStatus.PickedUp;
            }
        }
        else if (request.Status == DeliveryRunStatus.InTransit)
        {
            foreach (var order in run.Orders.Where(o => o.Status is OrderStatus.Confirmed or OrderStatus.PickedUp))
            {
                order.Status = OrderStatus.InTransit;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(LogisticsMappings.ToDto(run));
    }
}
