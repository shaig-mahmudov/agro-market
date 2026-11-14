using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Products;

public static class CreateProduct
{
    public sealed record Request(
        string Category,
        string Title,
        string? Description,
        decimal AvailableStockKg,
        decimal MinOrderKg,
        decimal BasePricePerKg,
        ProductMetadataDto? Metadata = null,
        string[]? ImageUrls = null,
        IReadOnlyList<PriceTierInput>? PriceTiers = null);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.AvailableStockKg).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MinOrderKg).GreaterThan(0);
            RuleFor(x => x.BasePricePerKg).GreaterThan(0);

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

    public static async Task<Results<Created<ProductDto>, ProblemHttpResult>> Handle(
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var farmerId = principal.GetUserId();
        var farmer = await db.Users
            .AsNoTracking()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == farmerId, cancellationToken);

        if (farmer is null || !farmer.IsVerified)
        {
            return TypedResults.Problem(
                title: "Verification required",
                detail: "Only verified farmers can publish produce listings.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var now = DateTimeOffset.UtcNow;
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            FarmerId = farmerId,
            Category = request.Category.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            AvailableStockKg = decimal.Round(request.AvailableStockKg, 2),
            MinOrderKg = decimal.Round(request.MinOrderKg, 2),
            BasePricePerKg = decimal.Round(request.BasePricePerKg, 2),
            Metadata = ProductMappings.ToEntity(request.Metadata),
            ImageUrls = request.ImageUrls?.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToArray() ?? [],
            IsActive = true,
            IsDeleted = false,
            UpdatedAt = now,
            PriceTiers = request.PriceTiers?
                .Select(t => new PriceTier
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    MinQuantityKg = decimal.Round(t.MinQuantityKg, 2),
                    PricePerKg = decimal.Round(t.PricePerKg, 2)
                })
                .ToList() ?? []
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        var farmerSummary = new FarmerSummaryDto(
            farmer.Id,
            farmer.FullName,
            farmer.BusinessProfile?.BusinessName,
            farmer.BusinessProfile?.Region);

        var dto = ProductMappings.ToDto(product, farmerSummary);
        return TypedResults.Created($"/api/products/{product.Id}", dto);
    }
}
