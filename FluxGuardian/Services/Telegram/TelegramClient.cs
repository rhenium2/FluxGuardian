using FluxGuardian.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FluxGuardian.Services.Telegram;

public class TelegramClient
{
    private readonly TelegramBotClient _botClient;

    public TelegramClient(string botToken)
    {
        _botClient = new TelegramBotClient(botToken);
    }

    public void SendMessage(long chatId, string message, int? replyToMessageId = null)
    {
        if (chatId <= 0 || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            _botClient.SendTextMessageAsync(chatId, message, replyToMessageId: replyToMessageId,
                parseMode: ParseMode.Markdown).Wait();
            Logger.Log($"Sent message '{message}' to chatId: {chatId}");
            Logger.LogMessage($"Sent message '{message}' to chatId: {chatId}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log($"Received exception {e}");
            Logger.LogMessage($"Received exception {e}");
        }
    }

    public void StartReceiving(Action<ITelegramBotClient, Update, CancellationToken> updateHandler,
        Action<ITelegramBotClient, Exception, CancellationToken> errorHandler)
    {
        _botClient.StartReceiving(updateHandler, errorHandler);
    }
}