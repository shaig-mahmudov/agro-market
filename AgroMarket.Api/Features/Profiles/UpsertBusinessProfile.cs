using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Profiles;

public static class UpsertBusinessProfile
{
    public sealed record Request(
        string? FullName,
        string BusinessName,
        string Region,
        string Address,
        decimal? Latitude = null,
        decimal? Longitude = null,
        string? PreferredDeliveryWindow = null,
        string? TaxId = null);

    public sealed record Response(
        Guid UserId,
        string PhoneNumber,
        string FullName,
        UserRole Role,
        bool IsVerified,
        long? TelegramChatId,
        BusinessProfileDto? BusinessProfile);

    public sealed record BusinessProfileDto(
        string BusinessName,
        string Region,
        string Address,
        decimal? Latitude,
        decimal? Longitude,
        string? PreferredDeliveryWindow,
        string? TaxId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FullName)
                .MaximumLength(150);

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Region)
                .NotEmpty()
                .MaximumLength(120);

            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(400);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90m, 90m)
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180m, 180m)
                .When(x => x.Longitude.HasValue);

            RuleFor(x => x.PreferredDeliveryWindow)
                .MaximumLength(64);

            RuleFor(x => x.TaxId)
                .MaximumLength(64);
        }
    }

    public static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleGet(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "User not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(ToResponse(user));
    }

    public static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleUpsert(
        Request request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var user = await db.Users
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "User not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (user.BusinessProfile is null)
        {
            user.BusinessProfile = new BusinessProfile
            {
                UserId = user.Id
            };
            db.BusinessProfiles.Add(user.BusinessProfile);
        }

        user.BusinessProfile.BusinessName = request.BusinessName.Trim();
        user.BusinessProfile.Region = request.Region.Trim();
        user.BusinessProfile.Address = request.Address.Trim();
        user.BusinessProfile.Latitude = request.Latitude;
        user.BusinessProfile.Longitude = request.Longitude;
        user.BusinessProfile.PreferredDeliveryWindow = request.PreferredDeliveryWindow?.Trim();
        user.BusinessProfile.TaxId = request.TaxId?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToResponse(user));
    }

    private static Response ToResponse(User user) => new(
        user.Id,
        user.PhoneNumber,
        user.FullName,
        user.Role,
        user.IsVerified,
        user.TelegramChatId,
        user.BusinessProfile is null
            ? null
            : new BusinessProfileDto(
                user.BusinessProfile.BusinessName,
                user.BusinessProfile.Region,
                user.BusinessProfile.Address,
                user.BusinessProfile.Latitude,
                user.BusinessProfile.Longitude,
                user.BusinessProfile.PreferredDeliveryWindow,
                user.BusinessProfile.TaxId));
}
