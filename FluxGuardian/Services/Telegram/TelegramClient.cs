using FluxGuardian.Helpers;
using FluxGuardian.Models;
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

    public void Init()
    {
        _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync);
    }

    private void HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var command = messageText.ToLowerInvariant();
        Logger.LogOutput($"Received '{messageText}' from {message.From.Username}:{chatId}");
        Logger.LogMessage($"{message.From.Username}:{message.Chat.Id} {messageText}");

        var user = UserService.FindTelegramUser(chatId);
        if (command.StartsWith("/"))
            HandleCommands(message, command);
        else if (user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
            HandleCommands(message, user.ActiveCommand);
    }

    public void HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Logger.LogOutput($"Received exception {exception.ToString()}");
    }

    private void HandleCommands(Message message, string command)
    {
        var commandContext = GetCommandContext(message);

        switch (command)
        {
            case "":
                break;
            case "/start":
                CommandService.HandleStartCommand(commandContext);
                break;
            case "/help":
                CommandService.HandleHelpCommand(commandContext);
                break;
            case "/addnode":
                CommandService.HandleAddNodeCommand(commandContext);
                break;
            case "/status":
                CommandService.HandleStatusCommand(commandContext);
                break;
            case "/mynodes":
                CommandService.HandleMyNodesCommand(commandContext);
                break;
            case "/removeallnodes":
                CommandService.HandleRemoveAllNodesCommand(commandContext);
                break;
            case "/admin/users":
                CommandService.HandleAdminUsersCommand(commandContext);
                break;
            case string when command.StartsWith("/admin/deleteuser"):
                CommandService.HandleAdminDeleteUserCommand(commandContext);
                break;
            case string when command.StartsWith("/admin/sendmessage"):
                CommandService.HandleAdminSendMessageCommand(commandContext);
                break;
            case string when command.StartsWith("/admin/broadcastmessage"):
                CommandService.HandleAdminBroadcastMessageCommand(commandContext);
                break;
            case string when command.StartsWith("/admin/addnode"):
                CommandService.HandleAdminAddNodeCommand(commandContext);
                break;
        }
    }

    private CommandContext GetCommandContext(Message message)
    {
        return new CommandContext
        {
            Message = message.Text,
            Username = message.From.Username,
            UserId = message.Chat.Id.ToString(),
            ContextKind = ContextKind.Telegram,
            TelegramClient = this,
            MessageId = message.MessageId.ToString()
        };
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
            Logger.LogOutput($"Sent message '{message}' to chatId: {chatId}");
            Logger.LogMessage($"Sent message '{message}' to chatId: {chatId}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.LogOutput($"Received exception {e}");
            Logger.LogMessage($"Received exception {e}");
        }
    }
}