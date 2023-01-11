using FluxGuardian.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FluxGuardian.Services;

public class TelegramClient
{
    private readonly string _botToken;
    private readonly TelegramBotClient botClient;

    public TelegramClient(string botToken)
    {
        _botToken = botToken;
        botClient = new TelegramBotClient(_botToken);
    }

    public async Task SendMessage(long chatId, string text)
    {
        await botClient.SendTextMessageAsync(new ChatId(chatId), text);
        Logger.Log($"Sent message '{text}' to chatId: {chatId}");
    }

    public void StartReceiving()
    {
        botClient.StartReceiving<TelegramMessageHandler>();
    }
}