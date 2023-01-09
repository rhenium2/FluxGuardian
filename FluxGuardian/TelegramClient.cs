using System.Text;
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
    }
    
    public async Task SendMessage(string text)
    {
        await botClient.SendTextMessageAsync(new ChatId(_chatId), text);
        Logger.Log($"Sent message '{text}' to chatId: {_chatId}");
    }
    
    public void StartReceiving()
    {
        botClient.StartReceiving<TelegramMessageHandler>();
    }
}

public class TelegramMessageHandler : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;
        
        var chatId = message.Chat.Id;
        Logger.Log($"Received a '{messageText}' from {message.From.Username} message in chat {chatId}.");

        if (messageText.ToLowerInvariant().Equals("status"))
        {
            var builder = new StringBuilder();
            foreach (var lastStatus in NodeGuard.LastStatus)
            {
                builder.Append($"node {lastStatus.Key.ToString()} is {lastStatus.Value} " + Environment.NewLine);
            }

            await botClient.SendTextMessageAsync(chatId, builder.ToString(), replyToMessageId: message.MessageId);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Logger.Log($"Received exception {exception.Message}");
        return Task.FromException(exception);
    }
}