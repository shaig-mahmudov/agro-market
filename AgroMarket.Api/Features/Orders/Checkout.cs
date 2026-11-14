using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Products;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Orders;

public static class Checkout
{
    public sealed record CartLineInput(
        Guid ProductId,
        decimal WeightKg);

    public sealed record Request(
        IReadOnlyList<CartLineInput> Items,
        string? DeliveryAddress = null,
        DateOnly? ScheduledDeliveryDate = null,
        PaymentMethod PaymentMethod = PaymentMethod.CashOnDelivery,
        FulfillmentType FulfillmentType = FulfillmentType.PlatformCourier);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Checkout requires at least one product item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.WeightKg).GreaterThan(0);
            });

            RuleFor(x => x.DeliveryAddress).MaximumLength(400);
            RuleFor(x => x.PaymentMethod).IsInEnum();
            RuleFor(x => x.FulfillmentType).IsInEnum();
        }
    }

    public static async Task<Results<Created<CheckoutGroupDto>, ProblemHttpResult>> Handle(
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IOptions<MarketplaceOptions> options,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        return await ExecuteCheckoutAsync(userId, request, db, options.Value, cancellationToken);
    }

    public static async Task<Results<Created<CheckoutGroupDto>, ProblemHttpResult>> ExecuteCheckoutAsync(
        Guid restaurantId,
        Request request,
        AppDbContext db,
        MarketplaceOptions opts,
        CancellationToken cancellationToken)
    {
        var restaurant = await db.Users
            .AsNoTracking()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == restaurantId, cancellationToken);

        if (restaurant is null || !restaurant.IsVerified)
        {
            return TypedResults.Problem(
                title: "Verification required",
                detail: "Only verified accounts can place marketplace orders.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (restaurant.Role is not (UserRole.Restaurant or UserRole.Admin))
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Only restaurant buyers can submit checkout orders.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var deliveryAddress = !string.IsNullOrWhiteSpace(request.DeliveryAddress)
            ? request.DeliveryAddress.Trim()
            : restaurant.BusinessProfile?.Address;

        if (string.IsNullOrWhiteSpace(deliveryAddress))
        {
            return TypedResults.Problem(
                title: "Delivery address required",
                detail: "Provide a deliveryAddress or save one in your Business Profile first.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        var (scheduledDate, cutoffAt, cutoffError) = ResolveDeliverySchedule(now, request.ScheduledDeliveryDate, opts);
        if (cutoffError is not null)
        {
            return TypedResults.Problem(
                title: "Daily cutoff passed",
                detail: cutoffError,
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Merge duplicate product lines cleanly so stock & tier thresholds apply to total kg per product.
        var mergedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CartLineInput(g.Key, decimal.Round(g.Sum(x => x.WeightKg), 2)))
            .ToList();

        var productIds = mergedItems.Select(i => i.ProductId).ToList();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var products = await db.Products
            .NotDeleted()
            .Include(p => p.PriceTiers)
            .Include(p => p.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var line in mergedItems)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                return TypedResults.Problem(
                    title: "Product not found",
                    detail: $"Product '{line.ProductId}' does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (!product.IsActive || !product.Farmer.IsVerified)
            {
                return TypedResults.Problem(
                    title: "Product unavailable",
                    detail: $"'{product.Title}' is not currently active for ordering.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (line.WeightKg < product.MinOrderKg)
            {
                return TypedResults.Problem(
                    title: "Minimum order weight not met",
                    detail: $"'{product.Title}' requires a minimum order of {product.MinOrderKg} kg.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (product.AvailableStockKg < line.WeightKg)
            {
                return TypedResults.Problem(
                    title: "Insufficient stock",
                    detail: $"'{product.Title}' only has {product.AvailableStockKg} kg available.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        var farmerLines = mergedItems
            .GroupBy(line => products[line.ProductId].FarmerId)
            .ToList();

        var totalDeliveryFee = request.FulfillmentType == FulfillmentType.BuyerPickup
            ? 0m
            : decimal.Round(opts.DefaultDeliveryFee, 2);

        var checkoutGroup = new CheckoutGroup
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            DeliveryAddress = deliveryAddress,
            ScheduledDeliveryDate = scheduledDate,
            CutoffAt = cutoffAt,
            TotalDeliveryFee = totalDeliveryFee,
            CreatedAt = now
        };

        db.CheckoutGroups.Add(checkoutGroup);

        var orderExpiresAt = now.AddMinutes(opts.OrderConfirmExpiryMinutes);
        var perFarmerDeliveryFees = SplitEvenly(totalDeliveryFee, farmerLines.Count);

        for (var idx = 0; idx < farmerLines.Count; idx++)
        {
            var farmerGroup = farmerLines[idx];
            var orderId = Guid.NewGuid();
            var orderItems = new List<OrderItem>();
            decimal estimatedProduceTotal = 0m;

            foreach (var line in farmerGroup)
            {
                var product = products[line.ProductId];
                var unitPrice = ProductMappings.CalculateUnitPrice(
                    product.BasePricePerKg,
                    product.PriceTiers,
                    line.WeightKg);

                product.AvailableStockKg = decimal.Round(product.AvailableStockKg - line.WeightKg, 2);
                product.UpdatedAt = now;

                var lineAmount = decimal.Round(unitPrice * line.WeightKg, 2);
                estimatedProduceTotal += lineAmount;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = product.Id,
                    ProductTitleSnapshot = product.Title,
                    UnitPriceApplied = unitPrice,
                    EstimatedWeightKg = line.WeightKg
                });
            }

            var platformFee = decimal.Round(estimatedProduceTotal * opts.PlatformFeeRate, 2);
            var deliveryFee = perFarmerDeliveryFees[idx];
            var totalWithFees = estimatedProduceTotal + platformFee + deliveryFee;

            var order = new Order
            {
                Id = orderId,
                CheckoutGroupId = checkoutGroup.Id,
                RestaurantId = restaurantId,
                FarmerId = farmerGroup.Key,
                FulfillmentType = request.FulfillmentType,
                Status = OrderStatus.Pending,
                EstimatedTotalAmount = estimatedProduceTotal,
                PlatformFeeAmount = platformFee,
                DeliveryFeeAmount = deliveryFee,
                FinalTotalAmount = totalWithFees,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                ExpiresAt = orderExpiresAt,
                CreatedAt = now,
                Items = orderItems
            };

            checkoutGroup.Orders.Add(order);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return TypedResults.Problem(
                title: "Concurrent stock update",
                detail: "Stock for one or more items changed during checkout. Please review availability and retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Attach already-loaded User summaries purely for DTO projection after commit.
        var farmerById = products.Values
            .Select(p => p.Farmer)
            .DistinctBy(f => f.Id)
            .ToDictionary(f => f.Id);

        foreach (var order in checkoutGroup.Orders)
        {
            order.Restaurant = restaurant;
            if (farmerById.TryGetValue(order.FarmerId, out var farmer))
            {
                order.Farmer = farmer;
            }
        }

        var dto = OrderMappings.ToCheckoutGroupDto(checkoutGroup);
        return TypedResults.Created($"/api/orders/checkout-groups/{checkoutGroup.Id}", dto);
    }

    private static (DateOnly ScheduledDate, DateTimeOffset CutoffAt, string? Error) ResolveDeliverySchedule(
        DateTimeOffset nowUtc,
        DateOnly? requestedDate,
        MarketplaceOptions opts)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(opts.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);
        var localToday = DateOnly.FromDateTime(localNow.Date);
        var todayCutoffLocal = new DateTime(
            localToday.Year,
            localToday.Month,
            localToday.Day,
            opts.DailyCutoffHourLocal,
            0,
            0,
            DateTimeKind.Unspecified);
        var todayCutoffOffset = new DateTimeOffset(todayCutoffLocal, tz.GetUtcOffset(todayCutoffLocal));

        var earliestDate = localNow <= todayCutoffOffset
            ? localToday.AddDays(1)
            : localToday.AddDays(2);

        if (requestedDate.HasValue)
        {
            if (requestedDate.Value < earliestDate)
            {
                return (
                    earliestDate,
                    todayCutoffOffset.ToUniversalTime(),
                    $"Daily cutoff ({opts.DailyCutoffHourLocal:00}:00 {tz.Id}) has passed for {requestedDate.Value:yyyy-MM-dd}. Earliest delivery date is {earliestDate:yyyy-MM-dd}.");
            }

            var targetCutoffDate = requestedDate.Value.AddDays(-1);
            var targetCutoffLocal = new DateTime(
                targetCutoffDate.Year,
                targetCutoffDate.Month,
                targetCutoffDate.Day,
                opts.DailyCutoffHourLocal,
                0,
                0,
                DateTimeKind.Unspecified);
            var targetCutoffOffset = new DateTimeOffset(targetCutoffLocal, tz.GetUtcOffset(targetCutoffLocal));
            return (requestedDate.Value, targetCutoffOffset.ToUniversalTime(), null);
        }

        var cutoffDate = earliestDate.AddDays(-1);
        var cutoffLocal = new DateTime(
            cutoffDate.Year,
            cutoffDate.Month,
            cutoffDate.Day,
            opts.DailyCutoffHourLocal,
            0,
            0,
            DateTimeKind.Unspecified);
        var cutoffOffset = new DateTimeOffset(cutoffLocal, tz.GetUtcOffset(cutoffLocal));
        return (earliestDate, cutoffOffset.ToUniversalTime(), null);
    }

    private static decimal[] SplitEvenly(decimal total, int count)
    {
        if (count <= 0) return [];
        var result = new decimal[count];
        var baseShare = decimal.Round(total / count, 2, MidpointRounding.ToZero);
        decimal assigned = 0m;
        for (var i = 0; i < count; i++)
        {
            if (i == count - 1)
            {
                result[i] = total - assigned;
            }
            else
            {
                result[i] = baseShare;
                assigned += baseShare;
            }
        }
        return result;
    }
}
