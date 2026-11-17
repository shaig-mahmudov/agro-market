using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Media;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/media")
            .WithTags("Media");

        group.MapPost("/upload", HandleUpload)
            .RequireAuthorization(AuthorizationPolicies.VerifiedUser)
            .DisableAntiforgery()
            .WithName("UploadMediaImage")
            .WithSummary("Upload a PNG/JPG/JPEG/WebP image, convert to WebP, and store in PostgreSQL");

        group.MapGet("/{id}", HandleGetMedia)
            .AllowAnonymous()
            .WithName("GetMediaImage")
            .WithSummary("Serve a WebP image stored in PostgreSQL by its ID");

        return api;
    }

    public static async Task<Results<Ok<MediaUploadResult>, ProblemHttpResult>> HandleUpload(
        IFormFile? file,
        string? folder,
        HttpContext httpContext,
        ClaimsPrincipal principal,
        AppDbContext db,
        IImageStorageService imageStorage,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return TypedResults.Problem(
                title: "Missing image file",
                detail: "Multipart form field 'file' is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var userId = principal.GetUserId();
        var publicBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}";

        try
        {
            var result = await imageStorage.ConvertToWebpAndStoreAsync(
                db,
                userId,
                string.IsNullOrWhiteSpace(folder) ? "products" : folder,
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

    public static async Task<IResult> HandleGetMedia(
        string id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var rawId = id.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? id[..^5]
            : id;

        if (!Guid.TryParse(rawId, out var assetId))
        {
            return TypedResults.Problem(
                title: "Invalid media ID",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var etag = $"\"{assetId:N}\"";
        if (httpContext.Request.Headers.IfNoneMatch.Contains(etag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var asset = await db.MediaAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken);

        if (asset is null)
        {
            return TypedResults.NotFound();
        }

        httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        httpContext.Response.Headers.ETag = etag;

        return TypedResults.File(asset.Data, asset.ContentType);
    }
}
