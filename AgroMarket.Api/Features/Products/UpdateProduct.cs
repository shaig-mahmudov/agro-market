using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Products;

public static class UpdateProduct
{
    public sealed record Request(
        string? Category = null,
        string? Title = null,
        string? Description = null,
        decimal? AvailableStockKg = null,
        decimal? MinOrderKg = null,
        decimal? BasePricePerKg = null,
        ProductMetadataDto? Metadata = null,
        string[]? ImageUrls = null,
        bool? IsActive = null,
        IReadOnlyList<PriceTierInput>? PriceTiers = null,
        uint? RowVersion = null);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Category).NotEmpty().MaximumLength(80).When(x => x.Category is not null);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200).When(x => x.Title is not null);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.AvailableStockKg).GreaterThanOrEqualTo(0).When(x => x.AvailableStockKg.HasValue);
            RuleFor(x => x.MinOrderKg).GreaterThan(0).When(x => x.MinOrderKg.HasValue);
            RuleFor(x => x.BasePricePerKg).GreaterThan(0).When(x => x.BasePricePerKg.HasValue);

            RuleFor(x => x.ImageUrls)
                .Must(urls => urls is null || urls.Length <= 8)
                .WithMessage("A product can have at most 8 images.");

            RuleFor(x => x.PriceTiers)
                .Must(ProductMappings.HasUniqueTierQuantities)
                .WithMessage("PriceTiers must have unique MinQuantityKg thresholds.");

            RuleForEach(x => x.PriceTiers).ChildRules(tier =>
            {
                tier.RuleFor(t => t.MinQuantityKg).GreaterThan(0);
                tier.RuleFor(t => t.PricePerKg).GreaterThan(0);
            });
        }
    }

    public static async Task<Results<Ok<ProductDto>, ProblemHttpResult>> HandleUpdate(
        Guid id,
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var product = await db.Products
            .NotDeleted()
            .Include(p => p.PriceTiers)
            .Include(p => p.Farmer)
                .ThenInclude(f => f.BusinessProfile)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && product.FarmerId != userId)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "You can only update your own produce listings.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (request.RowVersion.HasValue && request.RowVersion.Value != product.RowVersion)
        {
            return TypedResults.Problem(
                title: "Concurrency conflict",
                detail: "This product was modified concurrently. Refresh and retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (request.Category is not null) product.Category = request.Category.Trim();
        if (request.Title is not null) product.Title = request.Title.Trim();
        if (request.Description is not null) product.Description = request.Description.Trim();
        if (request.AvailableStockKg.HasValue) product.AvailableStockKg = decimal.Round(request.AvailableStockKg.Value, 2);
        if (request.MinOrderKg.HasValue) product.MinOrderKg = decimal.Round(request.MinOrderKg.Value, 2);
        if (request.BasePricePerKg.HasValue) product.BasePricePerKg = decimal.Round(request.BasePricePerKg.Value, 2);
        if (request.Metadata is not null) product.Metadata = ProductMappings.ToEntity(request.Metadata);
        if (request.ImageUrls is not null)
        {
            product.ImageUrls = request.ImageUrls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .ToArray();
        }
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        if (request.PriceTiers is not null)
        {
            db.PriceTiers.RemoveRange(product.PriceTiers);
            product.PriceTiers.Clear();
            foreach (var tier in request.PriceTiers)
            {
                product.PriceTiers.Add(new PriceTier
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    MinQuantityKg = decimal.Round(tier.MinQuantityKg, 2),
                    PricePerKg = decimal.Round(tier.PricePerKg, 2)
                });
            }
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                title: "Concurrency conflict",
                detail: "This product was updated by another operation. Refresh and retry.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var farmerSummary = new FarmerSummaryDto(
            product.Farmer.Id,
            product.Farmer.FullName,
            product.Farmer.BusinessProfile?.BusinessName,
            product.Farmer.BusinessProfile?.Region);

        return TypedResults.Ok(ProductMappings.ToDto(product, farmerSummary));
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> HandleDelete(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var role = principal.GetUserRole();

        var product = await db.Products
            .NotDeleted()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role != UserRole.Admin && product.FarmerId != userId)
        {
            return TypedResults.Problem(
                title: "Forbidden",
                detail: "You can only delete your own produce listings.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        product.IsDeleted = true;
        product.IsActive = false;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
