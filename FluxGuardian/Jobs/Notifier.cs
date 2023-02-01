using FluxGuardian.Models;

namespace FluxGuardian.Services;

public static class Notifier
{
    public static void NotifyUser(User user, string message)
    {
        var context = user.GetUserContextKind();
        if (context == ContextKind.Telegram)
        {
            Program.TelegramClient.SendMessage(user.TelegramChatId, message);
        }

        if (context == ContextKind.Discord)
        {
            Program.DiscordClient.SendMessage(user.DiscordId, message);
        }
    }

    public static void NotifyContext(CommandContext context, string message)
    {
        if (context.ContextKind == ContextKind.Telegram)
        {
            Program.TelegramClient.SendMessage(Convert.ToInt64(context.UserId), message);
        }

        if (context.ContextKind == ContextKind.Discord)
        {
            Program.DiscordClient.SendMessage(context.DiscordChannel, message);
        }
    }
}