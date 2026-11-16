using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Infrastructure.Auth;
using Microsoft.Extensions.Options;

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

        group.MapPost("/seed", async (
            AppDbContext db,
            IOptions<AuthBootstrapOptions> bootstrapOptions,
            IOptions<SeedOptions> seedOptions,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var result = await DatabaseSeeder.SeedAsync(
                db,
                bootstrapOptions.Value,
                new SeedOptions { Enabled = true, SeedSampleData = seedOptions.Value.SeedSampleData || true },
                loggerFactory.CreateLogger("DatabaseSeeder"),
                cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName("AdminSeedSampleData")
        .WithSummary("Idempotently seed bootstrap accounts, business profiles, products, orders, and milk-run delivery runs");

        return api;
    }
}
