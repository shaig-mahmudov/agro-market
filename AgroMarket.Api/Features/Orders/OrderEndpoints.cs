using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapPost("/checkout", Checkout.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithValidation<Checkout.Request>()
            .WithName("Checkout")
            .WithSummary("Split multi-farmer cart into a CheckoutGroup and per-farmer Pending Orders");

        group.MapPost("/{checkoutGroupId:guid}/repeat", RepeatOrder.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithName("RepeatOrder")
            .WithSummary("1-click repeat order from a previous CheckoutGroup");

        group.MapGet("/checkout-groups", ListOrders.HandleListCheckoutGroups)
            .WithName("ListCheckoutGroups")
            .WithSummary("List restaurant checkout groups with split farmer orders");

        group.MapGet("/checkout-groups/{id:guid}", ListOrders.HandleGetCheckoutGroup)
            .WithName("GetCheckoutGroup")
            .WithSummary("Get a single checkout group and its split farmer orders");

        group.MapGet("/", ListOrders.HandleListOrders)
            .WithName("ListOrders")
            .WithSummary("List orders scoped to the authenticated restaurant, farmer, or admin");

        group.MapGet("/{id:guid}", ListOrders.HandleGetOrder)
            .WithName("GetOrderById")
            .WithSummary("Get details for a single order");

        return api;
    }
}
