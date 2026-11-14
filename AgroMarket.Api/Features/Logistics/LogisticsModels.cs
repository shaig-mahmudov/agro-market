using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Features.Orders;

namespace AgroMarket.Api.Features.Logistics;

public sealed record DeliveryRunOrderDto(
    Guid Id,
    Guid CheckoutGroupId,
    OrderStatus Status,
    FulfillmentType FulfillmentType,
    string? FarmerName,
    string? FarmerAddress,
    string? RestaurantName,
    string? DeliveryAddress,
    decimal EstimatedTotalAmount,
    decimal FinalTotalAmount,
    decimal TotalEstimatedWeightKg,
    IReadOnlyList<OrderItemDto> Items);

public sealed record DeliveryRunDto(
    Guid Id,
    string DriverName,
    string DriverPhone,
    string? VehicleNote,
    DateOnly RunDate,
    DeliveryRunStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DeliveryRunOrderDto> Orders);

public static class LogisticsMappings
{
    public static DeliveryRunDto ToDto(DeliveryRun run) => new(
        run.Id,
        run.DriverName,
        run.DriverPhone,
        run.VehicleNote,
        run.RunDate,
        run.Status,
        run.CreatedAt,
        run.Orders
            .OrderBy(o => o.CreatedAt)
            .Select(o => new DeliveryRunOrderDto(
                o.Id,
                o.CheckoutGroupId,
                o.Status,
                o.FulfillmentType,
                o.Farmer?.BusinessProfile?.BusinessName ?? o.Farmer?.FullName,
                o.Farmer?.BusinessProfile?.Address,
                o.Restaurant?.BusinessProfile?.BusinessName ?? o.Restaurant?.FullName,
                o.CheckoutGroup?.DeliveryAddress,
                o.EstimatedTotalAmount,
                o.FinalTotalAmount,
                o.Items.Sum(i => i.EstimatedWeightKg),
                o.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.ProductTitleSnapshot,
                    i.UnitPriceApplied,
                    i.EstimatedWeightKg,
                    i.ActualDeliveredWeightKg,
                    i.RejectedWeightKg,
                    i.ProofPhotoUrl)).ToList()))
            .ToList());
}
