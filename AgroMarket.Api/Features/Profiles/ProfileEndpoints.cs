using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Profiles;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/profiles")
            .WithTags("Profiles")
            .RequireAuthorization();

        group.MapGet("/me", UpsertBusinessProfile.HandleGet)
            .WithName("GetMyProfile")
            .WithSummary("Get the authenticated user and business profile");

        group.MapPut("/me", UpsertBusinessProfile.HandleUpsert)
            .WithValidation<UpsertBusinessProfile.Request>()
            .WithName("UpsertMyProfile")
            .WithSummary("Create or update the authenticated user's business profile");

        return api;
    }
}
