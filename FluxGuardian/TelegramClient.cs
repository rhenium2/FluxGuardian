using FluxGuardian.Helpers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace FluxGuardian;

public class TelegramClient
{
    private readonly string _botToken;
    private readonly long _chatId;
    private readonly TelegramBotClient botClient;

    public TelegramClient(string botToken, long chatId)
    {
        _botToken = botToken;
        _chatId = chatId;
        botClient = new TelegramBotClient(_botToken);
        StartReceiving();
    }
    
    public async Task SendMessage(string text)
    {
        await botClient.SendTextMessageAsync(new ChatId(_chatId), text);
        Logger.Log($"Sent message '{text}' to chatId: {_chatId}");
    }
    
    public void StartReceiving()
    {
        botClient.StartReceiving<TelegramHandler>();
    }
}

public class TelegramHandler : IUpdateHandler
{
    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return Task.CompletedTask;
        // Only process text messages
        if (message.Text is not { } messageText)
            return Task.CompletedTask;
        
        var chatId = message.Chat.Id;
        Logger.Log($"Received a '{messageText}' from {message.From.Username} message in chat {chatId}.");

        return Task.CompletedTask;
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Logger.Log($"Received exception {exception.Message}");
        return Task.FromException(exception);
    }
}