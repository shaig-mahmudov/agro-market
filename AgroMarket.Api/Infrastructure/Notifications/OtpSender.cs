using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Infrastructure.Notifications;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public string Provider { get; set; } = "Console";
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public bool ReturnCodeInResponse { get; set; }
}

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public string BotUsername { get; set; } = "AgroMarketBot";
    public string GatewayApiToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public interface IOtpSender
{
    Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}

public sealed class ConsoleOtpSender(ILogger<ConsoleOtpSender> logger) : IOtpSender
{
    public Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV OTP] Phone={PhoneNumber} Code={OtpCode}", phoneNumber, code);
        return Task.CompletedTask;
    }
}

public sealed class TelegramGatewayOtpSender(
    HttpClient httpClient,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramGatewayOtpSender> logger) : IOtpSender
{
    private readonly TelegramOptions _telegram = telegramOptions.Value;

    public async Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_telegram.GatewayApiToken))
        {
            logger.LogWarning(
                "Telegram Gateway token is not configured; falling back to log output for {PhoneNumber}. Code={OtpCode}",
                phoneNumber,
                code);
            return;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gatewayapi.telegram.org/sendVerificationMessage");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _telegram.GatewayApiToken);
        request.Content = JsonContent.Create(new
        {
            phone_number = phoneNumber,
            code,
            ttl = 300
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
