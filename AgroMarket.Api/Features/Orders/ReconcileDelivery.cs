using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Products;
using AgroMarket.Api.Infrastructure.Notifications;
using AgroMarket.Api.Infrastructure.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Orders;

public static class ReconcileDelivery
{
    public sealed record ItemReconciliationInput(
        Guid OrderItemId,
        decimal ActualDeliveredWeightKg,
        decimal RejectedWeightKg = 0m,
        string? ProofPhotoUrl = null);

    public sealed record Request(IReadOnlyList<ItemReconciliationInput> Items);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Reconciliation requires all order items.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.OrderItemId).NotEmpty();
                item.RuleFor(i => i.ActualDeliveredWeightKg).GreaterThanOrEqualTo(0);
                item.RuleFor(i => i.RejectedWeightKg).GreaterThanOrEqualTo(0);
                item.RuleFor(i => i.RejectedWeightKg)
                    .LessThanOrEqualTo(i => i.ActualDeliveredWeightKg)
                    .WithMessage("RejectedWeightKg cannot exceed ActualDeliveredWeightKg.");
                item.RuleFor(i => i.ProofPhotoUrl).MaximumLength(500);
            });
        }
    }

    public static async Task<Results<Ok<MediaUploadResult>, ProblemHttpResult>> HandleUploadProofPhoto(
        Guid id,
        IFormFile? file,
        HttpContext httpContext,
        ClaimsPrincipal principal,
        AppDbContext db,
        IImageStorageService imageStorage,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();
        if (role is not (UserRole.Admin or UserRole.Driver or UserRole.Farmer or UserRole.Restaurant))
        {
            return TypedResults.Problem(title: "Forbidden", statusCode: StatusCodes.Status403Forbidden);
        }

        if (file is null)
        {
            return TypedResults.Problem(
                title: "Missing image file",
                detail: "Multipart form field 'file' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var publicBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}";

        try
        {
            var result = await imageStorage.ConvertToWebpAndStoreAsync(
                db,
                userId,
                $"delivery-proofs/{id:N}",
                file,
                publicBaseUrl,
                cancellationToken);

            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.Problem(
                title: "Invalid image upload",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<OrderDto>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IOptions<MarketplaceOptions> options,
        ITelegramNotifier telegramNotifier,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();
        var opts = options.Value;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

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

        var canReconcile = role is UserRole.Admin or UserRole.Driver
            || (role == UserRole.Farmer && order.FarmerId == userId && order.FulfillmentType == FulfillmentType.FarmerDelivery);

        if (!canReconcile)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "Only assigned drivers, admins, or self-delivering farmers can reconcile weights.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.PickedUp or OrderStatus.InTransit))
        {
            return TypedResults.Problem(
                title: "Order cannot be reconciled",
                detail: $"Order is currently {order.Status}. Only Confirmed, PickedUp, or InTransit orders can be reconciled.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var inputsByItemId = new Dictionary<Guid, ItemReconciliationInput>();
        foreach (var input in request.Items)
        {
            if (!inputsByItemId.TryAdd(input.OrderItemId, input))
            {
                return TypedResults.Problem(
                    title: "Duplicate order item",
                    detail: $"OrderItem {input.OrderItemId} appeared multiple times.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        if (inputsByItemId.Count != order.Items.Count || order.Items.Any(i => !inputsByItemId.ContainsKey(i.Id)))
        {
            return TypedResults.Problem(
                title: "Incomplete item reconciliation",
                detail: "Every line item on the order must be included when reconciling weights.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var needsBuyerApproval = false;
        decimal reconciledProduceTotal = 0m;

        foreach (var item in order.Items)
        {
            var input = inputsByItemId[item.Id];
            var actualKg = decimal.Round(input.ActualDeliveredWeightKg, 2);
            var rejectedKg = decimal.Round(input.RejectedWeightKg, 2);
            var varianceKg = actualKg - item.EstimatedWeightKg;

            if (products.TryGetValue(item.ProductId, out var product))
            {
                if (varianceKg < 0m)
                {
                    // Return unfulfilled estimated weight back to farmer inventory.
                    product.AvailableStockKg = decimal.Round(product.AvailableStockKg + (-varianceKg), 2);
                    product.UpdatedAt = now;
                }
                else if (varianceKg > 0m)
                {
                    if (product.AvailableStockKg < varianceKg)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return TypedResults.Problem(
                            title: "Insufficient stock for over-delivery",
                            detail: $"'{item.ProductTitleSnapshot}' only has {product.AvailableStockKg:F2} kg additional stock remaining (max deliverable total: {item.EstimatedWeightKg + product.AvailableStockKg:F2} kg).",
                            statusCode: StatusCodes.Status409Conflict);
                    }

                    product.AvailableStockKg = decimal.Round(product.AvailableStockKg - varianceKg, 2);
                    product.UpdatedAt = now;
                }
            }

            var varianceRatio = item.EstimatedWeightKg > 0m
                ? Math.Abs(varianceKg) / item.EstimatedWeightKg
                : (actualKg > 0m ? 1m : 0m);

            if (varianceRatio > opts.VarianceToleranceRate || rejectedKg > 0m)
            {
                needsBuyerApproval = true;
            }

            var chargeableWeightKg = decimal.Round(actualKg - rejectedKg, 2);
            reconciledProduceTotal += decimal.Round(item.UnitPriceApplied * chargeableWeightKg, 2);

            item.ActualDeliveredWeightKg = actualKg;
            item.RejectedWeightKg = rejectedKg;
            item.ProofPhotoUrl = string.IsNullOrWhiteSpace(input.ProofPhotoUrl)
                ? null
                : input.ProofPhotoUrl.Trim();
        }

        var platformFee = decimal.Round(reconciledProduceTotal * opts.PlatformFeeRate, 2);
        order.PlatformFeeAmount = platformFee;
        order.FinalTotalAmount = reconciledProduceTotal + platformFee + order.DeliveryFeeAmount;

        if (needsBuyerApproval)
        {
            order.Status = OrderStatus.DeliveredPendingApproval;
            order.DeliveredAt = null;
        }
        else
        {
            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = now;
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
                title: "Concurrent stock update",
                detail: "Product stock changed while reconciling delivery. Please retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (order.Restaurant?.TelegramChatId is { } restaurantChatId)
        {
            var farmerName = order.Farmer?.BusinessProfile?.BusinessName ?? order.Farmer?.FullName ?? "Farmer";
            var summary = needsBuyerApproval
                ? $"Delivery reconciled (${order.FinalTotalAmount:F2}) with weight variance > ±10% or rejected items. Please review and approve/dispute in AgroMarket."
                : $"Delivery completed (${order.FinalTotalAmount:F2}) within ±10% tolerance.";
            await telegramNotifier.NotifyRestaurantOrderStatusAsync(
                order,
                restaurantChatId,
                farmerName,
                summary,
                cancellationToken);
        }

        return TypedResults.Ok(OrderMappings.ToOrderDto(order));
    }
}
