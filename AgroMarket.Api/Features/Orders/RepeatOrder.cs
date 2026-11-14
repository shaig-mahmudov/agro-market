using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Infrastructure.Notifications;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Orders;

public static class RepeatOrder
{
    public sealed record Request(
        string? DeliveryAddress = null,
        DateOnly? ScheduledDeliveryDate = null,
        PaymentMethod? PaymentMethod = null);

    public static async Task<Results<Created<CheckoutGroupDto>, ProblemHttpResult>> Handle(
        Guid checkoutGroupId,
        Request? request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IOptions<MarketplaceOptions> options,
        ITelegramNotifier telegramNotifier,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var sourceGroup = await db.CheckoutGroups
            .AsNoTracking()
            .Include(cg => cg.Orders)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(cg => cg.Id == checkoutGroupId, cancellationToken);

        if (sourceGroup is null)
        {
            return TypedResults.Problem(
                title: "Checkout group not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && sourceGroup.RestaurantId != userId)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "You can only repeat your own previous orders.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var lines = sourceGroup.Orders
            .SelectMany(o => o.Items)
            .Select(i => new Checkout.CartLineInput(i.ProductId, i.EstimatedWeightKg))
            .ToList();

        if (lines.Count == 0)
        {
            return TypedResults.Problem(
                title: "Empty order",
                detail: "No items were found in the original checkout group.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var firstOrder = sourceGroup.Orders.First();
        var checkoutRequest = new Checkout.Request(
            Items: lines,
            DeliveryAddress: request?.DeliveryAddress ?? sourceGroup.DeliveryAddress,
            ScheduledDeliveryDate: request?.ScheduledDeliveryDate,
            PaymentMethod: request?.PaymentMethod ?? firstOrder.PaymentMethod,
            FulfillmentType: firstOrder.FulfillmentType);

        return await Checkout.ExecuteCheckoutAsync(
            sourceGroup.RestaurantId,
            checkoutRequest,
            db,
            options.Value,
            cancellationToken,
            telegramNotifier);
    }
}
