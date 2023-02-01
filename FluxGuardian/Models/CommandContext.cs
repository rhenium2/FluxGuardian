using Discord.WebSocket;
using FluxGuardian.Services.Discord;
using FluxGuardian.Services.Telegram;

namespace FluxGuardian.Models;

public class CommandContext
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public ContextKind ContextKind { get; set; }
    public string Message { get; set; }
    public string MessageId { get; set; }
    public Dictionary<string, string> Arguments { get; set; }

    //TODO : delete
    public TelegramClient TelegramClient { get; set; }

    //TODO : delete
    public DiscordClient DiscordClient { get; set; }
    public ISocketMessageChannel DiscordChannel { get; set; }

    public CommandContext()
    {
        Arguments = new Dictionary<string, string>();
    }
}

public enum ContextKind
{
    Telegram,
    Discord
}