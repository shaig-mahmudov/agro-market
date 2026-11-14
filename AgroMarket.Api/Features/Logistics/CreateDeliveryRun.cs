using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Logistics;

public static class CreateDeliveryRun
{
    public sealed record Request(
        string DriverName,
        string DriverPhone,
        string? VehicleNote,
        DateOnly? RunDate,
        IReadOnlyList<Guid> OrderIds);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DriverName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.DriverPhone).NotEmpty().MaximumLength(32);
            RuleFor(x => x.VehicleNote).MaximumLength(300);
            RuleFor(x => x.OrderIds).NotEmpty().WithMessage("At least one confirmed order must be assigned.");
            RuleForEach(x => x.OrderIds).NotEmpty();
        }
    }

    public static async Task<Results<Created<DeliveryRunDto>, ProblemHttpResult>> Handle(
        Request request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.DriverPhone);
        if (!PhoneNumberNormalizer.IsValidE164(normalizedPhone))
        {
            return TypedResults.Problem(
                title: "Invalid driver phone",
                detail: "DriverPhone must be a valid E.164 phone number.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var distinctOrderIds = request.OrderIds.Distinct().ToList();
        var orders = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.CheckoutGroup)
            .Include(o => o.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Include(o => o.Restaurant)
                .ThenInclude(r => r.BusinessProfile)
            .Where(o => distinctOrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        if (orders.Count != distinctOrderIds.Count)
        {
            return TypedResults.Problem(
                title: "Orders not found",
                detail: "One or more requested order IDs do not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var invalidOrder = orders.FirstOrDefault(o => o.Status != OrderStatus.Confirmed || o.DeliveryRunId.HasValue);
        if (invalidOrder is not null)
        {
            return TypedResults.Problem(
                title: "Order ineligible for dispatch",
                detail: invalidOrder.DeliveryRunId.HasValue
                    ? $"Order {invalidOrder.Id} is already assigned to a delivery run."
                    : $"Order {invalidOrder.Id} is {invalidOrder.Status}; only Confirmed orders can be dispatched.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var runDate = request.RunDate
            ?? orders.Select(o => o.CheckoutGroup.ScheduledDeliveryDate).FirstOrDefault();
        if (runDate == default)
        {
            runDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        }

        var run = new DeliveryRun
        {
            Id = Guid.NewGuid(),
            DriverName = request.DriverName.Trim(),
            DriverPhone = normalizedPhone,
            VehicleNote = string.IsNullOrWhiteSpace(request.VehicleNote) ? null : request.VehicleNote.Trim(),
            RunDate = runDate,
            Status = DeliveryRunStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        db.DeliveryRuns.Add(run);

        foreach (var order in orders)
        {
            order.DeliveryRunId = run.Id;
            run.Orders.Add(order);
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return TypedResults.Created($"/api/logistics/runs/{run.Id}", LogisticsMappings.ToDto(run));
    }
}
