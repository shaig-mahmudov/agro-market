using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Infrastructure.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Auth;

public static class RefreshAuthToken
{
    public sealed record Request(string RefreshToken);

    public sealed record Response(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        VerifyOtp.UserSummary User);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .MinimumLength(20);
        }
    }

    public static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleRefresh(
        Request request,
        AppDbContext db,
        JwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        var tokenHash = SecretHasher.Sha256Hex(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= now)
        {
            return TypedResults.Problem(
                title: "Invalid refresh token",
                detail: "The refresh token is invalid, expired, or has already been revoked.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        existing.RevokedAt = now;
        var pair = tokenService.IssueTokenPair(existing.User);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = SecretHasher.Sha256Hex(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        var user = existing.User;
        return TypedResults.Ok(new Response(
            pair.AccessToken,
            pair.AccessTokenExpiresAt,
            pair.RefreshToken,
            pair.RefreshTokenExpiresAt,
            new VerifyOtp.UserSummary(
                user.Id,
                user.PhoneNumber,
                user.FullName,
                user.Role,
                user.IsVerified,
                user.TelegramChatId)));
    }

    public static async Task<NoContent> HandleLogout(
        Request request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tokenHash = SecretHasher.Sha256Hex(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        await db.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedAt, now),
                cancellationToken);

        return TypedResults.NoContent();
    }
}
