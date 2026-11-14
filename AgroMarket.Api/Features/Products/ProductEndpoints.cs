using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/products")
            .WithTags("Products");

        group.MapGet("/", ListProducts.HandleList)
            .WithName("ListProducts")
            .WithSummary("Browse and filter active verified farmer produce listings");

        group.MapGet("/mine", ListProducts.HandleListMine)
            .RequireAuthorization()
            .WithName("ListMyProducts")
            .WithSummary("List the authenticated farmer's own produce listings");

        group.MapGet("/{id:guid}", ListProducts.HandleGetById)
            .WithName("GetProductById")
            .WithSummary("Get details and tiered pricing for a single produce listing");

        group.MapPost("/upload-url", ProductMappings.HandlePresignUpload)
            .RequireAuthorization(AuthorizationPolicies.VerifiedFarmer)
            .WithValidation<PresignUploadRequest>()
            .WithName("CreateProductUploadUrl")
            .WithSummary("Generate a Cloudflare R2 presigned PUT URL for produce photo upload");

        group.MapPost("/", CreateProduct.Handle)
            .RequireAuthorization(AuthorizationPolicies.VerifiedFarmer)
            .WithValidation<CreateProduct.Request>()
            .WithName("CreateProduct")
            .WithSummary("Create a new produce listing with volume price tiers and metadata");

        group.MapPatch("/{id:guid}", UpdateProduct.HandleUpdate)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithValidation<UpdateProduct.Request>()
            .WithName("UpdateProduct")
            .WithSummary("Update stock, price tiers, metadata, or active status (optimistic concurrency)");

        group.MapDelete("/{id:guid}", UpdateProduct.HandleDelete)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .WithName("DeleteProduct")
            .WithSummary("Soft-delete a produce listing");

        return api;
    }
}
