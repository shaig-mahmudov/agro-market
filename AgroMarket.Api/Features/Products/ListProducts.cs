using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Products;

public static class ListProducts
{
    public static async Task<Ok<PagedResult<ProductDto>>> HandleList(
        AppDbContext db,
        string? category = null,
        string? region = null,
        string? search = null,
        Guid? farmerId = null,
        bool? isOrganic = null,
        decimal? minStockKg = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);

        var query = db.Products
            .AsNoTracking()
            .ActiveCatalog()
            .Where(p => p.Farmer.IsVerified);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Category, cat));
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            var reg = region.Trim();
            query = query.Where(p =>
                p.Farmer.BusinessProfile != null &&
                EF.Functions.ILike(p.Farmer.BusinessProfile.Region, reg));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, term) ||
                EF.Functions.ILike(p.Category, term) ||
                (p.Description != null && EF.Functions.ILike(p.Description, term)) ||
                (p.Farmer.BusinessProfile != null && EF.Functions.ILike(p.Farmer.BusinessProfile.BusinessName, term)));
        }

        if (farmerId.HasValue)
        {
            query = query.Where(p => p.FarmerId == farmerId.Value);
        }

        if (isOrganic.HasValue)
        {
            query = query.Where(p => p.Metadata.IsOrganic == isOrganic.Value);
        }

        if (minStockKg.HasValue)
        {
            query = query.Where(p => p.AvailableStockKg >= minStockKg.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(p => new ProductDto(
                p.Id,
                p.FarmerId,
                p.Category,
                p.Title,
                p.Description,
                p.AvailableStockKg,
                p.MinOrderKg,
                p.BasePricePerKg,
                new ProductMetadataDto(
                    p.Metadata.Variety,
                    p.Metadata.HarvestDate,
                    p.Metadata.Grade,
                    p.Metadata.Packaging,
                    p.Metadata.IsOrganic),
                p.ImageUrls,
                p.IsActive,
                p.UpdatedAt,
                p.RowVersion,
                p.PriceTiers
                    .OrderBy(t => t.MinQuantityKg)
                    .Select(t => new PriceTierDto(t.Id, t.MinQuantityKg, t.PricePerKg))
                    .ToList(),
                new FarmerSummaryDto(
                    p.Farmer.Id,
                    p.Farmer.FullName,
                    p.Farmer.BusinessProfile != null ? p.Farmer.BusinessProfile.BusinessName : null,
                    p.Farmer.BusinessProfile != null ? p.Farmer.BusinessProfile.Region : null)))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(PagedResult<ProductDto>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Ok<PagedResult<ProductDto>>> HandleListMine(
        ClaimsPrincipal principal,
        AppDbContext db,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var farmerId = principal.GetUserId();
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);

        var query = db.Products
            .AsNoTracking()
            .NotDeleted()
            .Where(p => p.FarmerId == farmerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(p => new ProductDto(
                p.Id,
                p.FarmerId,
                p.Category,
                p.Title,
                p.Description,
                p.AvailableStockKg,
                p.MinOrderKg,
                p.BasePricePerKg,
                new ProductMetadataDto(
                    p.Metadata.Variety,
                    p.Metadata.HarvestDate,
                    p.Metadata.Grade,
                    p.Metadata.Packaging,
                    p.Metadata.IsOrganic),
                p.ImageUrls,
                p.IsActive,
                p.UpdatedAt,
                p.RowVersion,
                p.PriceTiers
                    .OrderBy(t => t.MinQuantityKg)
                    .Select(t => new PriceTierDto(t.Id, t.MinQuantityKg, t.PricePerKg))
                    .ToList(),
                null))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(PagedResult<ProductDto>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Results<Ok<ProductDto>, ProblemHttpResult>> HandleGetById(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.Products
            .AsNoTracking()
            .NotDeleted()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(
                p.Id,
                p.FarmerId,
                p.Category,
                p.Title,
                p.Description,
                p.AvailableStockKg,
                p.MinOrderKg,
                p.BasePricePerKg,
                new ProductMetadataDto(
                    p.Metadata.Variety,
                    p.Metadata.HarvestDate,
                    p.Metadata.Grade,
                    p.Metadata.Packaging,
                    p.Metadata.IsOrganic),
                p.ImageUrls,
                p.IsActive,
                p.UpdatedAt,
                p.RowVersion,
                p.PriceTiers
                    .OrderBy(t => t.MinQuantityKg)
                    .Select(t => new PriceTierDto(t.Id, t.MinQuantityKg, t.PricePerKg))
                    .ToList(),
                new FarmerSummaryDto(
                    p.Farmer.Id,
                    p.Farmer.FullName,
                    p.Farmer.BusinessProfile != null ? p.Farmer.BusinessProfile.BusinessName : null,
                    p.Farmer.BusinessProfile != null ? p.Farmer.BusinessProfile.Region : null)))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? TypedResults.Problem(title: "Product not found", statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(item);
    }
}
