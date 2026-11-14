using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapGet("/users", VerifyUser.HandleListUsers)
            .WithName("AdminListUsers")
            .WithSummary("List marketplace users for onboarding review and verification");

        group.MapPatch("/users/{id:guid}/verify", VerifyUser.HandleVerify)
            .WithValidation<VerifyUser.Request>()
            .WithName("AdminVerifyUser")
            .WithSummary("Approve or update verification status and role for a user");

        group.MapPatch("/orders/{id:guid}/payment", MarkOrderPaid.Handle)
            .WithValidation<MarkOrderPaid.Request>()
            .WithName("AdminMarkOrderPayment")
            .WithSummary("Reconcile COD, Bank Transfer, or Weekly Invoice payment settlement");

        return api;
    }
}
