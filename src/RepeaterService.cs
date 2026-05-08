using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramRepeaterBot;

/// <summary>
/// Hosted background service that listens for updates and repeats plain-text
/// messages from the configured input chat to the configured output chat.
/// </summary>
public sealed class RepeaterService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly BotSettings _settings;
    private readonly ILogger<RepeaterService> _logger;

    public RepeaterService(
        ITelegramBotClient bot,
        IOptions<BotSettings> settings,
        ILogger<RepeaterService> logger)
    {
        _bot = bot;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Repeater started. Input chat: {Input}, Output chat: {Output}",
            _settings.InputChatId,
            _settings.OutputChatId);

        var receiverOptions = new ReceiverOptions
        {
            // Only care about plain-text messages
            AllowedUpdates = [UpdateType.Message],
            // Drop all pending updates accumulated while the bot was offline
            DropPendingUpdates = true,
        };

        await _bot.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        // We only registered for Message updates, but guard defensively.
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;

        // Silently ignore any chat that is not the configured input chat.
        if (chatId != _settings.InputChatId)
        {
            _logger.LogDebug("Ignored message from chat {ChatId}", chatId);
            return;
        }

        // We only handle plain text (including messages that contain only emojis).
        var text = message.Text;
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogDebug("Ignored non-text message from input chat");
            return;
        }

        _logger.LogInformation(
            "Forwarding message from {From}: {Preview}",
            message.From?.Username ?? chatId.ToString(),
            text.Length > 80 ? text[..80] + "…" : text);

        try
        {
            await bot.SendMessage(
                chatId: _settings.OutputChatId,
                text: text,
                parseMode: ParseMode.None,          // treat as plain text — don't interpret markdown
                cancellationToken: ct);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error while sending message to output chat");
        }
    }

    private Task HandlePollingErrorAsync(
        ITelegramBotClient bot,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is ApiRequestException apiEx)
            _logger.LogError(apiEx, "Telegram API error [{ErrorCode}]", apiEx.ErrorCode);
        else
            _logger.LogError(exception, "Polling error");

        return Task.CompletedTask;
    }
}
