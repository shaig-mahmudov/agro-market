using System.Security.Cryptography;
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

public static class SendOtp
{
    public sealed record Request(string PhoneNumber);

    public sealed record Response(
        string PhoneNumber,
        DateTimeOffset ExpiresAt,
        string? DevOtpCode = null);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Must(phone => PhoneNumberNormalizer.IsValidE164(PhoneNumberNormalizer.Normalize(phone)))
                .WithMessage("PhoneNumber must be a valid E.164 phone number (e.g., +994501234567).");
        }
    }

    public static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Request request,
        AppDbContext db,
        IOtpSender otpSender,
        IOptions<OtpOptions> otpOptions,
        IOptions<AuthBootstrapOptions> bootstrapOptions,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var opts = otpOptions.Value;
        var bootstrap = bootstrapOptions.Value;
        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var now = DateTimeOffset.UtcNow;

        // Invalidate any prior active OTPs for this phone so only the latest code is usable.
        await db.OtpChallenges
            .Where(o => o.PhoneNumber == normalizedPhone && o.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(o => o.ConsumedAt, now),
                cancellationToken);

        var isBootstrapDemo = bootstrap.EnableBootstrapDemoOtp && bootstrap.IsBootstrapPhone(normalizedPhone);
        var code = isBootstrapDemo && !string.IsNullOrWhiteSpace(bootstrap.BootstrapOtpCode)
            ? bootstrap.BootstrapOtpCode.Trim()
            : RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = now.AddMinutes(opts.ExpiryMinutes);

        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalizedPhone,
            CodeHash = SecretHasher.Sha256Hex($"{normalizedPhone}:{code}"),
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);

        if (!isBootstrapDemo || otpSender is ConsoleOtpSender)
        {
            await otpSender.SendOtpAsync(normalizedPhone, code, cancellationToken);
        }

        var returnCode = (environment.IsDevelopment() && opts.ReturnCodeInResponse) || isBootstrapDemo;
        var devCode = returnCode ? code : null;
        return TypedResults.Ok(new Response(normalizedPhone, expiresAt, devCode));
    }
}
