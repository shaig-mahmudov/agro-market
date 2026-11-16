using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using AgroMarket.Api.Infrastructure.Auth;
using AgroMarket.Api.Infrastructure.Notifications;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Auth;

public static class VerifyOtp
{
    public sealed record Request(
        string PhoneNumber,
        string Code,
        string? FullName = null,
        UserRole? Role = null);

    public sealed record UserSummary(
        Guid Id,
        string PhoneNumber,
        string FullName,
        UserRole Role,
        bool IsVerified,
        long? TelegramChatId);

    public sealed record Response(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        bool IsNewUser,
        UserSummary User);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Must(phone => PhoneNumberNormalizer.IsValidE164(PhoneNumberNormalizer.Normalize(phone)))
                .WithMessage("PhoneNumber must be a valid E.164 phone number.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .Length(6)
                .Must(c => c.All(char.IsAsciiDigit))
                .WithMessage("Code must be a 6-digit numeric OTP.");

            RuleFor(x => x.FullName)
                .MaximumLength(150);

            RuleFor(x => x.Role)
                .Must(role => role is null or UserRole.Farmer or UserRole.Restaurant)
                .WithMessage("Self-registration role must be Farmer or Restaurant.");
        }
    }

    public static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Request request,
        AppDbContext db,
        JwtTokenService tokenService,
        IOptions<OtpOptions> otpOptions,
        IOptions<AuthBootstrapOptions> bootstrapOptions,
        CancellationToken cancellationToken)
    {
        var opts = otpOptions.Value;
        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var now = DateTimeOffset.UtcNow;

        var challenge = await db.OtpChallenges
            .Where(o => o.PhoneNumber == normalizedPhone && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null || challenge.ExpiresAt <= now)
        {
            return TypedResults.Problem(
                title: "Invalid or expired OTP",
                detail: "No active OTP challenge was found for this phone number.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (challenge.AttemptCount >= opts.MaxAttempts)
        {
            challenge.ConsumedAt = now;
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Problem(
                title: "OTP attempts exceeded",
                detail: "Maximum verification attempts exceeded. Please request a new OTP.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var expectedHash = SecretHasher.Sha256Hex($"{normalizedPhone}:{request.Code}");
        if (!string.Equals(challenge.CodeHash, expectedHash, StringComparison.Ordinal))
        {
            challenge.AttemptCount += 1;
            if (challenge.AttemptCount >= opts.MaxAttempts)
            {
                challenge.ConsumedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Problem(
                title: "Invalid OTP",
                detail: "The verification code is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        challenge.ConsumedAt = now;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);

        var isNewUser = user is null;
        if (user is null)
        {
            var preset = DatabaseSeeder.DefaultBootstrapAccounts
                .FirstOrDefault(a => PhoneNumberNormalizer.Normalize(a.PhoneNumber) == normalizedPhone);

            var isBootstrapAdmin = bootstrapOptions.Value.BootstrapAdminPhones
                .Select(PhoneNumberNormalizer.Normalize)
                .Contains(normalizedPhone, StringComparer.Ordinal);
            var isBootstrapFarmer = bootstrapOptions.Value.BootstrapFarmerPhones
                .Select(PhoneNumberNormalizer.Normalize)
                .Contains(normalizedPhone, StringComparer.Ordinal);
            var isBootstrapRestaurant = bootstrapOptions.Value.BootstrapRestaurantPhones
                .Select(PhoneNumberNormalizer.Normalize)
                .Contains(normalizedPhone, StringComparer.Ordinal);
            var isBootstrapDriver = bootstrapOptions.Value.BootstrapDriverPhones
                .Select(PhoneNumberNormalizer.Normalize)
                .Contains(normalizedPhone, StringComparer.Ordinal);

            var resolvedRole = preset?.Role
                ?? (isBootstrapAdmin ? UserRole.Admin
                    : isBootstrapDriver ? UserRole.Driver
                    : isBootstrapFarmer ? UserRole.Farmer
                    : isBootstrapRestaurant ? UserRole.Restaurant
                    : (request.Role ?? UserRole.Restaurant));

            var resolvedVerified = preset?.IsVerified
                ?? (isBootstrapAdmin || isBootstrapDriver || isBootstrapFarmer || isBootstrapRestaurant);

            var resolvedFullName = !string.IsNullOrWhiteSpace(request.FullName)
                ? request.FullName.Trim()
                : (preset?.FullName ?? normalizedPhone);

            user = new User
            {
                Id = preset?.Id ?? Guid.NewGuid(),
                PhoneNumber = normalizedPhone,
                FullName = resolvedFullName,
                Role = resolvedRole,
                IsVerified = resolvedVerified,
                CreatedAt = now
            };

            db.Users.Add(user);

            if (preset is not null)
            {
                db.BusinessProfiles.Add(new BusinessProfile
                {
                    UserId = user.Id,
                    BusinessName = preset.BusinessName,
                    Region = preset.Region,
                    Address = preset.Address,
                    Latitude = preset.Latitude,
                    Longitude = preset.Longitude,
                    PreferredDeliveryWindow = preset.PreferredDeliveryWindow,
                    TaxId = preset.TaxId
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.FullName) && user.FullName == user.PhoneNumber)
        {
            user.FullName = request.FullName.Trim();
        }

        var pair = tokenService.IssueTokenPair(user);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SecretHasher.Sha256Hex(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(
            pair.AccessToken,
            pair.AccessTokenExpiresAt,
            pair.RefreshToken,
            pair.RefreshTokenExpiresAt,
            isNewUser,
            new UserSummary(
                user.Id,
                user.PhoneNumber,
                user.FullName,
                user.Role,
                user.IsVerified,
                user.TelegramChatId)));
    }
}
