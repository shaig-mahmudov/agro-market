using AgroMarket.Api.Common;

namespace AgroMarket.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/send-otp", SendOtp.Handle)
            .RequireRateLimiting(RateLimitPolicies.OtpSend)
            .WithValidation<SendOtp.Request>()
            .WithName("SendOtp")
            .WithSummary("Send a 6-digit login/registration OTP to a phone number");

        group.MapPost("/verify-otp", VerifyOtp.Handle)
            .RequireRateLimiting(RateLimitPolicies.OtpSend)
            .WithValidation<VerifyOtp.Request>()
            .WithName("VerifyOtp")
            .WithSummary("Verify OTP code and issue JWT access + refresh tokens");

        group.MapPost("/refresh", RefreshAuthToken.HandleRefresh)
            .WithValidation<RefreshAuthToken.Request>()
            .WithName("RefreshToken")
            .WithSummary("Rotate refresh token and issue a fresh JWT access token");

        group.MapPost("/logout", RefreshAuthToken.HandleLogout)
            .WithValidation<RefreshAuthToken.Request>()
            .WithName("Logout")
            .WithSummary("Revoke a refresh token");

        group.MapGet("/me", LinkTelegram.HandleMe)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Return the currently authenticated user profile");

        group.MapGet("/telegram-deep-link", LinkTelegram.HandleCreateDeepLink)
            .RequireAuthorization()
            .WithName("CreateTelegramDeepLink")
            .WithSummary("Generate a signed Telegram Bot deep-link for linking notifications");

        group.MapPost("/link-telegram", LinkTelegram.HandleLink)
            .WithValidation<LinkTelegram.LinkRequest>()
            .WithName("LinkTelegram")
            .WithSummary("Link a TelegramChatId via authenticated request or signed deep-link token");

        return api;
    }
}
