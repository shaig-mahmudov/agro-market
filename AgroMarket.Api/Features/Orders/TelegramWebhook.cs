using System.Text.Json.Serialization;
using AgroMarket.Api.Database;
using AgroMarket.Api.Infrastructure.Auth;
using AgroMarket.Api.Infrastructure.Notifications;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgroMarket.Api.Features.Orders;

public static class TelegramWebhook
{
    public sealed record Update(
        [property: JsonPropertyName("update_id")] long UpdateId,
        [property: JsonPropertyName("message")] Message? Message,
        [property: JsonPropertyName("callback_query")] CallbackQuery? CallbackQuery);

    public sealed record Message(
        [property: JsonPropertyName("message_id")] long MessageId,
        [property: JsonPropertyName("chat")] Chat Chat,
        [property: JsonPropertyName("text")] string? Text);

    public sealed record Chat(
        [property: JsonPropertyName("id")] long Id);

    public sealed record CallbackQuery(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("from")] TelegramUser From,
        [property: JsonPropertyName("data")] string? Data);

    public sealed record TelegramUser(
        [property: JsonPropertyName("id")] long Id);

    public static async Task<Results<Ok<object>, UnauthorizedHttpResult>> Handle(
        Update update,
        HttpRequest httpRequest,
        AppDbContext db,
        JwtTokenService tokenService,
        ITelegramNotifier notifier,
        IOptions<TelegramOptions> telegramOptions,
        CancellationToken cancellationToken)
    {
        var configuredSecret = telegramOptions.Value.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            var headerSecret = httpRequest.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.Equals(headerSecret, configuredSecret, StringComparison.Ordinal))
            {
                return TypedResults.Unauthorized();
            }
        }

        if (update.Message?.Text is { } text && text.StartsWith("/start ", StringComparison.OrdinalIgnoreCase))
        {
            var token = text[7..].Trim();
            if (!tokenService.TryValidateTelegramLinkToken(token, out var userId))
            {
                await notifier.SendTextAsync(
                    update.Message.Chat.Id,
                    "This AgroMarket link has expired. Generate a fresh link from your profile.",
                    cancellationToken);
                return TypedResults.Ok<object>(new { handled = "invalid_token" });
            }

            var conflict = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.TelegramChatId == update.Message.Chat.Id && u.Id != userId, cancellationToken);

            if (conflict)
            {
                await notifier.SendTextAsync(
                    update.Message.Chat.Id,
                    "This Telegram account is already linked to another AgroMarket user.",
                    cancellationToken);
                return TypedResults.Ok<object>(new { handled = "chat_conflict" });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is not null)
            {
                user.TelegramChatId = update.Message.Chat.Id;
                await db.SaveChangesAsync(cancellationToken);
                await notifier.SendTextAsync(
                    update.Message.Chat.Id,
                    $"Connected! You will now receive AgroMarket order alerts for {user.FullName}.",
                    cancellationToken);
            }

            return TypedResults.Ok<object>(new { handled = "linked" });
        }

        if (update.CallbackQuery?.Data is { } data && data.StartsWith("order:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = data.Split(':');
            if (parts.Length == 3 && Guid.TryParse(parts[2], out var orderId))
            {
                var accept = string.Equals(parts[1], "accept", StringComparison.OrdinalIgnoreCase);
                var reject = string.Equals(parts[1], "reject", StringComparison.OrdinalIgnoreCase);

                if (!accept && !reject)
                {
                    await notifier.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Unknown action.", cancellationToken);
                    return TypedResults.Ok<object>(new { handled = "unknown_action" });
                }

                var farmer = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.TelegramChatId == update.CallbackQuery.From.Id, cancellationToken);

                if (farmer is null)
                {
                    await notifier.AnswerCallbackQueryAsync(
                        update.CallbackQuery.Id,
                        "Account not linked. Link Telegram in your AgroMarket profile first.",
                        cancellationToken);
                    return TypedResults.Ok<object>(new { handled = "unlinked_user" });
                }

                var result = await FarmerRespond.RespondToOrderAsync(
                    orderId,
                    farmer.Id,
                    isAdmin: false,
                    accept,
                    db,
                    cancellationToken);

                var replyText = result.Result is Ok<OrderDto>
                    ? (accept ? "Order accepted for morning pickup!" : "Order rejected and stock restored.")
                    : "Order could not be updated (already processed or expired).";

                await notifier.AnswerCallbackQueryAsync(update.CallbackQuery.Id, replyText, cancellationToken);
                return TypedResults.Ok<object>(new { handled = accept ? "accepted" : "rejected" });
            }
        }

        return TypedResults.Ok<object>(new { handled = "ignored" });
    }
}
