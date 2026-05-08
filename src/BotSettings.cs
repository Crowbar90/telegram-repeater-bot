namespace TelegramRepeaterBot;

/// <summary>
/// Strongly-typed settings read from configuration / environment variables.
/// </summary>
public sealed class BotSettings
{
    public const string SectionName = "Bot";

    /// <summary>Telegram Bot API token (injected via the TELEGRAM__TOKEN env-var).</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Chat ID that the bot listens to for incoming messages.</summary>
    public long InputChatId { get; init; }

    /// <summary>Chat ID where the bot forwards messages.</summary>
    public long OutputChatId { get; init; }

    /// <summary>Optional Topic ID (message thread) in the output chat.</summary>
    public int? OutputTopicId { get; init; }
}