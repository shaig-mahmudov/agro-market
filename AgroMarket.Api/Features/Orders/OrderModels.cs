using AgroMarket.Api.Database.Entities;

namespace AgroMarket.Api.Features.Orders;

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductTitleSnapshot,
    decimal UnitPriceApplied,
    decimal EstimatedWeightKg,
    decimal? ActualDeliveredWeightKg,
    decimal? RejectedWeightKg,
    string? ProofPhotoUrl);

public sealed record OrderDto(
    Guid Id,
    Guid CheckoutGroupId,
    Guid RestaurantId,
    Guid FarmerId,
    string? FarmerName,
    string? FarmerTaxId,
    string? RestaurantName,
    string? RestaurantTaxId,
    Guid? DeliveryRunId,
    FulfillmentType FulfillmentType,
    OrderStatus Status,
    decimal EstimatedTotalAmount,
    decimal PlatformFeeAmount,
    decimal DeliveryFeeAmount,
    decimal FinalTotalAmount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    string? DisputeNote,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record CheckoutGroupDto(
    Guid Id,
    Guid RestaurantId,
    string? RestaurantName,
    string? RestaurantTaxId,
    string DeliveryAddress,
    DateOnly ScheduledDeliveryDate,
    DateTimeOffset CutoffAt,
    decimal TotalDeliveryFee,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderDto> Orders);

public static class OrderMappings
{
    public static OrderDto ToOrderDto(Order order) => new(
        order.Id,
        order.CheckoutGroupId,
        order.RestaurantId,
        order.FarmerId,
        order.Farmer?.BusinessProfile?.BusinessName ?? order.Farmer?.FullName,
        order.Farmer?.BusinessProfile?.TaxId,
        order.Restaurant?.BusinessProfile?.BusinessName ?? order.Restaurant?.FullName,
        order.Restaurant?.BusinessProfile?.TaxId,
        order.DeliveryRunId,
        order.FulfillmentType,
        order.Status,
        order.EstimatedTotalAmount,
        order.PlatformFeeAmount,
        order.DeliveryFeeAmount,
        order.FinalTotalAmount,
        order.PaymentMethod,
        order.PaymentStatus,
        order.DisputeNote,
        order.ExpiresAt,
        order.ConfirmedAt,
        order.DeliveredAt,
        order.CancelledAt,
        order.CreatedAt,
        order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductTitleSnapshot,
            i.UnitPriceApplied,
            i.EstimatedWeightKg,
            i.ActualDeliveredWeightKg,
            i.RejectedWeightKg,
            i.ProofPhotoUrl)).ToList());

    public static CheckoutGroupDto ToCheckoutGroupDto(CheckoutGroup group)
    {
        var firstOrderRestaurant = group.Restaurant ?? group.Orders.FirstOrDefault()?.Restaurant;
        return new(
            group.Id,
            group.RestaurantId,
            firstOrderRestaurant?.BusinessProfile?.BusinessName ?? firstOrderRestaurant?.FullName,
            firstOrderRestaurant?.BusinessProfile?.TaxId,
            group.DeliveryAddress,
            group.ScheduledDeliveryDate,
            group.CutoffAt,
            group.TotalDeliveryFee,
            group.CreatedAt,
            group.Orders.OrderBy(o => o.CreatedAt).Select(ToOrderDto).ToList());
    }
}
