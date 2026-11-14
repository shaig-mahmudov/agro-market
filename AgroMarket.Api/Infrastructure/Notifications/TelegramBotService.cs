using System.Net.Http.Json;
using System.Text;
using AgroMarket.Api.Database.Entities;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Infrastructure.Notifications;

public interface ITelegramNotifier
{
    Task NotifyFarmerNewOrderAsync(Order order, long chatId, string restaurantName, CancellationToken cancellationToken);
    Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken);
    Task AnswerCallbackQueryAsync(string callbackQueryId, string text, CancellationToken cancellationToken);
}

public sealed class TelegramBotService(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotService> logger) : ITelegramNotifier
{
    private readonly TelegramOptions _telegram = options.Value;

    public async Task NotifyFarmerNewOrderAsync(
        Order order,
        long chatId,
        string restaurantName,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"New AgroMarket Order #{order.Id.ToString()[..8]}");
        sb.AppendLine($"Buyer: {restaurantName}");
        sb.AppendLine($"Estimated Total: ${order.EstimatedTotalAmount:F2}");
        sb.AppendLine($"Confirm before: {order.ExpiresAt:HH:mm} UTC");
        sb.AppendLine("Items:");
        foreach (var item in order.Items)
        {
            sb.AppendLine($"- {item.ProductTitleSnapshot}: {item.EstimatedWeightKg} kg @ ${item.UnitPriceApplied:F2}/kg");
        }

        if (string.IsNullOrWhiteSpace(_telegram.BotToken))
        {
            logger.LogInformation(
                "[TELEGRAM STUB] ChatId={ChatId} OrderId={OrderId} Text={Text}",
                chatId,
                order.Id,
                sb.ToString().ReplaceLineEndings(" | "));
            return;
        }

        var payload = new
        {
            chat_id = chatId,
            text = sb.ToString(),
            reply_markup = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "Accept Order", callback_data = $"order:accept:{order.Id:N}" },
                        new { text = "Reject Order", callback_data = $"order:reject:{order.Id:N}" }
                    }
                }
            }
        };

        await PostBotMethodAsync("sendMessage", payload, cancellationToken);
    }

    public async Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_telegram.BotToken))
        {
            logger.LogInformation("[TELEGRAM STUB] ChatId={ChatId} Message={Text}", chatId, text);
            return;
        }

        await PostBotMethodAsync("sendMessage", new { chat_id = chatId, text }, cancellationToken);
    }

    public async Task AnswerCallbackQueryAsync(string callbackQueryId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_telegram.BotToken))
        {
            logger.LogInformation("[TELEGRAM STUB] Callback={CallbackId} Text={Text}", callbackQueryId, text);
            return;
        }

        await PostBotMethodAsync("answerCallbackQuery", new { callback_query_id = callbackQueryId, text }, cancellationToken);
    }

    private async Task PostBotMethodAsync(string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_telegram.BotToken}/{method}";
            using var response = await httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Telegram Bot API {Method} returned {StatusCode}", method, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to call Telegram Bot API {Method}", method);
        }
    }
}
