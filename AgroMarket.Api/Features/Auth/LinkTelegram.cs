using System.Security.Claims;
using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Infrastructure.Auth;
using AgroMarket.Api.Infrastructure.Notifications;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Auth;

public static class LinkTelegram
{
    public sealed record DeepLinkResponse(
        string Token,
        string DeepLinkUrl,
        DateTimeOffset ExpiresAt);

    public sealed record LinkRequest(
        long TelegramChatId,
        string? DeepLinkToken = null);

    public sealed record LinkResponse(
        Guid UserId,
        long TelegramChatId);

    public sealed class Validator : AbstractValidator<LinkRequest>
    {
        public Validator()
        {
            RuleFor(x => x.TelegramChatId)
                .NotEqual(0)
                .WithMessage("TelegramChatId must be a valid non-zero Telegram chat ID.");
        }
    }

    public static Ok<DeepLinkResponse> HandleCreateDeepLink(
        ClaimsPrincipal principal,
        JwtTokenService tokenService,
        IOptions<TelegramOptions> telegramOptions)
    {
        var userId = principal.GetUserId();
        var ttl = TimeSpan.FromMinutes(15);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var token = tokenService.CreateTelegramLinkToken(userId, ttl);
        var botUsername = telegramOptions.Value.BotUsername.TrimStart('@');
        var url = $"https://t.me/{botUsername}?start={token}";

        return TypedResults.Ok(new DeepLinkResponse(token, url, expiresAt));
    }

    public static async Task<Results<Ok<LinkResponse>, ProblemHttpResult>> HandleLink(
        LinkRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        JwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        Guid targetUserId;

        if (!string.IsNullOrWhiteSpace(request.DeepLinkToken))
        {
            if (!tokenService.TryValidateTelegramLinkToken(request.DeepLinkToken, out targetUserId))
            {
                return TypedResults.Problem(
                    title: "Invalid or expired link token",
                    detail: "The Telegram deep-link token is invalid or expired.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
        else if (principal.Identity?.IsAuthenticated == true)
        {
            targetUserId = principal.GetUserId();
        }
        else
        {
            return TypedResults.Problem(
                title: "Authentication required",
                detail: "Provide a valid bearer token or a signed Telegram deep-link token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var conflict = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.TelegramChatId == request.TelegramChatId && u.Id != targetUserId, cancellationToken);

        if (conflict)
        {
            return TypedResults.Problem(
                title: "Telegram account already linked",
                detail: "This Telegram chat ID is already linked to another AgroMarket account.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null)
        {
            return TypedResults.Problem(
                title: "User not found",
                detail: "Target user account does not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }

        user.TelegramChatId = request.TelegramChatId;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new LinkResponse(user.Id, request.TelegramChatId));
    }

    public static async Task<Results<Ok<VerifyOtp.UserSummary>, ProblemHttpResult>> HandleMe(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new VerifyOtp.UserSummary(
                u.Id,
                u.PhoneNumber,
                u.FullName,
                u.Role,
                u.IsVerified,
                u.TelegramChatId))
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? TypedResults.Problem(
                title: "User not found",
                statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(user);
    }
}
