using AgroMarket.Api.Common;
using AgroMarket.Api.Features.Products;

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

        group.MapPatch("/{id:guid}/respond", FarmerRespond.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithValidation<FarmerRespond.Request>()
            .WithName("FarmerRespondToOrder")
            .WithSummary("Accept or reject a pending farmer order (restores stock on reject)");

        group.MapPost("/{id:guid}/proof-photo", ReconcileDelivery.HandleUploadProofPhoto)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .DisableAntiforgery()
            .WithName("UploadDeliveryProofPhoto")
            .WithSummary("Upload a doorstep delivery/scale proof photo (PNG/JPG/JPEG/WebP), convert to WebP, and store in PostgreSQL");

        group.MapPatch("/{id:guid}/reconcile", ReconcileDelivery.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithValidation<ReconcileDelivery.Request>()
            .WithName("ReconcileDelivery")
            .WithSummary("Record actual and rejected delivery weights with ±10% variance guardrail");

        group.MapPatch("/{id:guid}/confirm", ConfirmReceipt.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithValidation<ConfirmReceipt.Request>()
            .WithName("ConfirmDeliveryReceipt")
            .WithSummary("Buyer approves or disputes an order awaiting variance/quality approval");

        api.MapPost("/webhooks/telegram", TelegramWebhook.Handle)
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithName("TelegramWebhook")
            .WithSummary("Handle Telegram bot updates (/start deep-link and order inline callbacks)");

        return api;
    }
}
