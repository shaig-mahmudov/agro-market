using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Infrastructure.Storage;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgroMarket.Api.Features.Products;

public sealed record PriceTierInput(decimal MinQuantityKg, decimal PricePerKg);

public sealed record PriceTierDto(Guid Id, decimal MinQuantityKg, decimal PricePerKg);

public sealed record ProductMetadataDto(
    string? Variety,
    DateOnly? HarvestDate,
    string? Grade,
    string? Packaging,
    bool IsOrganic);

public sealed record FarmerSummaryDto(
    Guid Id,
    string FullName,
    string? BusinessName,
    string? Region);

public sealed record ProductDto(
    Guid Id,
    Guid FarmerId,
    string Category,
    string Title,
    string? Description,
    decimal AvailableStockKg,
    decimal MinOrderKg,
    decimal BasePricePerKg,
    ProductMetadataDto Metadata,
    string[] ImageUrls,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    uint RowVersion,
    IReadOnlyList<PriceTierDto> PriceTiers,
    FarmerSummaryDto? Farmer = null);

public sealed record PresignUploadRequest(
    string FileName,
    string ContentType);

public sealed class PresignUploadRequestValidator : AbstractValidator<PresignUploadRequest>
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/webp",
        "image/jpeg",
        "image/png"
    };

    public PresignUploadRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedTypes.Contains(ct))
            .WithMessage("ContentType must be one of: image/webp, image/jpeg, image/png.");
    }
}

public static class ProductMappings
{
    public static ProductMetadata ToEntity(ProductMetadataDto? dto)
        => dto is null
            ? new ProductMetadata()
            : new ProductMetadata
            {
                Variety = dto.Variety?.Trim(),
                HarvestDate = dto.HarvestDate,
                Grade = dto.Grade?.Trim(),
                Packaging = dto.Packaging?.Trim(),
                IsOrganic = dto.IsOrganic
            };

    public static ProductDto ToDto(Product product, FarmerSummaryDto? farmer = null) => new(
        product.Id,
        product.FarmerId,
        product.Category,
        product.Title,
        product.Description,
        product.AvailableStockKg,
        product.MinOrderKg,
        product.BasePricePerKg,
        new ProductMetadataDto(
            product.Metadata.Variety,
            product.Metadata.HarvestDate,
            product.Metadata.Grade,
            product.Metadata.Packaging,
            product.Metadata.IsOrganic),
        product.ImageUrls,
        product.IsActive,
        product.UpdatedAt,
        product.RowVersion,
        product.PriceTiers
            .OrderBy(t => t.MinQuantityKg)
            .Select(t => new PriceTierDto(t.Id, t.MinQuantityKg, t.PricePerKg))
            .ToList(),
        farmer);

    public static decimal CalculateUnitPrice(decimal basePricePerKg, IEnumerable<PriceTier> tiers, decimal quantityKg)
    {
        var matchedTier = tiers
            .Where(t => quantityKg >= t.MinQuantityKg)
            .OrderByDescending(t => t.MinQuantityKg)
            .FirstOrDefault();

        return matchedTier?.PricePerKg ?? basePricePerKg;
    }

    public static bool HasUniqueTierQuantities(IReadOnlyList<PriceTierInput>? tiers)
    {
        if (tiers is null || tiers.Count == 0)
        {
            return true;
        }

        return tiers.Select(t => t.MinQuantityKg).Distinct().Count() == tiers.Count;
    }

    public static Ok<PresignedUploadResult> HandlePresignUpload(
        PresignUploadRequest request,
        ClaimsPrincipal principal,
        IObjectStorageService storageService)
    {
        var userId = principal.GetUserId();
        var result = storageService.CreatePresignedPutUrl(
            userId,
            "products",
            request.FileName,
            request.ContentType);

        return TypedResults.Ok(result);
    }
}
