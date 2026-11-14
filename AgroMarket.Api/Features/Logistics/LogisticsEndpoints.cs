using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Logistics;

public static class LogisticsEndpoints
{
    public static IEndpointRouteBuilder MapLogisticsEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/logistics/runs")
            .WithTags("Logistics")
            .RequireAuthorization(AuthorizationPolicies.AdminOrDriver);

        group.MapPost("/", CreateDeliveryRun.Handle)
            .RequireAuthorization(AuthorizationPolicies.Admin)
            .WithValidation<CreateDeliveryRun.Request>()
            .WithName("CreateDeliveryRun")
            .WithSummary("Admin batches confirmed orders into a morning milk-run DeliveryRun");

        group.MapPatch("/{id:guid}/status", UpdateRunStatus.Handle)
            .WithValidation<UpdateRunStatus.Request>()
            .WithName("UpdateDeliveryRunStatus")
            .WithSummary("Advance delivery run status and cascade PickedUp/InTransit to assigned orders");

        group.MapGet("/", ListDeliveryRuns.HandleList)
            .WithName("ListDeliveryRuns")
            .WithSummary("List morning milk-run delivery runs");

        group.MapGet("/{id:guid}", ListDeliveryRuns.HandleGetById)
            .WithName("GetDeliveryRunById")
            .WithSummary("Get details for a single delivery run");

        return api;
    }
}
