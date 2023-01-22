using System.Text;
using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FluxGuardian.Services.Telegram;

public class TelegramCommandHandler
{
    private readonly TelegramClient _telegramClient;
    private const int maximumNodeCount = 4;
    public TelegramCommandHandler(TelegramClient telegramClient)
    {
        _telegramClient = telegramClient;
    }

    public void Init()
    {
        _telegramClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync);
    }

    public void HandleUpdateAsync(ITelegramBotClient botClient, Update update,
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
        Logger.Log($"Received '{messageText}' from {message.From.Username}:{chatId}");
        Logger.LogMessage($"{message.From.Username}:{message.Chat.Id} {messageText}");
        
        var user = UserService.FindTelegramUser(chatId);
        if (command.StartsWith("/"))
        {
           HandleCommands(message, command);
        }
        else if(user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
        {
            HandleCommands(message, user.ActiveCommand);
        }
    }
    
    public void HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Logger.Log($"Received exception {exception.ToString()}");
    }
    
    private void HandleCommands(Message message, string command)
    {
        switch (command)
        {
            case "":
                break;
            case "/start":
                HandleStartCommand(message);
                break;
            case "/addnode":
                HandleAddNodeCommand(message);
                break;
            case "/status":
                HandleStatusCommand(message);
                break;
            case "/mynodes":
                HandleMyNodesCommand(message);
                break;
            case "/removeallnodes":
                HandleRemoveAllNodesCommand(message);
                break;
            case "/admin/users":
                HandleAdminUsersCommand(message);
                break;
            case string when command.StartsWith("/admin/deleteuser"):
                HandleAdminDeleteUserCommand(message);
                break;
            case string when command.StartsWith("/admin/sendmessage"):
                HandleAdminSendMessageCommand(message);
                break;
        }
    }

    private void HandleAdminSendMessageCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (chatId != Program.FluxConfig.MChatId)
        {
            return;
        }
        if (user is null)
        {
            return;
        }

        var commandItems = message.Text.Trim()
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        
        var sendingChatId = Convert.ToInt64(commandItems[1].Trim());
        var sendingMessage = string.Join(' ', commandItems.Skip(2));
        var sendingUser = UserService.FindTelegramUser(sendingChatId);
        if (sendingUser is not null)
        {
            _telegramClient.SendMessage(sendingChatId, sendingMessage);
            _telegramClient.SendMessage(chatId, $"message \"{sendingMessage}\" sent to {sendingChatId}");
        }
    }
    
    private void HandleAdminDeleteUserCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (chatId != Program.FluxConfig.MChatId)
        {
            return;
        }
        if (user is null)
        {
            return;
        }

        var deletingChatId = Convert.ToInt64(message.Text.Split(' ')[1].Trim());
        var deletingUser = UserService.FindTelegramUser(deletingChatId);
        if (deletingUser is not null)
        {
            Database.DefaultInstance.Users.Delete(deletingUser.Id);
            _telegramClient.SendMessage(chatId, "user deleted");
        }
    }

    public void HandleStartCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (user is not null)
        {
            UserService.ResetActiveCommand(user);
            UserService.RemoveAllNodes(user);
        }

        var text = new StringBuilder("Hellow from FluxGuardian bot 🤖");
        text.AppendLine();
        text.AppendLine("This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you.");
        text.AppendLine();
        text.AppendLine($"Currently, you can add up to {maximumNodeCount} nodes.");
        text.AppendLine();
        text.AppendLine("This bot is in Beta and is available \"AS IS\" without any warranty of any kind.");
        text.AppendLine();
        text.AppendLine("type /addnode to add a new node for me to monitor. At any point if you are stuck, type /start and you can start fresh.");

        _telegramClient.SendMessage(chatId, text.ToString(), message.MessageId);
    }

    public void HandleAddNodeCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId) ?? UserService.CreateTelegramUser(username, chatId);

        if (user.Nodes.Count >= maximumNodeCount)
        {
            _telegramClient.SendMessage(chatId, "sorry, you have reached the maximum number of nodes");
            return;
        }
        
        if (string.IsNullOrEmpty(user.ActiveCommand))
        {
            user.ActiveCommand = message.Text;
            Database.DefaultInstance.Users.Update(user);
        }

        if (!user.ActiveCommandParams.ContainsKey("ip"))
        {
            user.ActiveCommandParams["ip"] = String.Empty;
            Database.DefaultInstance.Users.Update(user);
            _telegramClient.SendMessage(chatId, "sure, what is this node IP address?");
        }
        else if (string.IsNullOrEmpty(user.ActiveCommandParams["ip"]))
        {
            user.ActiveCommandParams["ip"] = message.Text.Trim();
            Database.DefaultInstance.Users.Update(user);
        }
        
        if (!string.IsNullOrEmpty(user.ActiveCommandParams["ip"]) && !user.ActiveCommandParams.ContainsKey("port"))
        {
            user.ActiveCommandParams["port"] = String.Empty;
            Database.DefaultInstance.Users.Update(user);
            _telegramClient.SendMessage(chatId, "what is this node's API port? (usually 16127)");
        }
        else if (!string.IsNullOrEmpty(user.ActiveCommandParams["ip"]) && string.IsNullOrEmpty(user.ActiveCommandParams["port"]))
        {
            user.ActiveCommandParams["port"] = message.Text.Trim();
            Database.DefaultInstance.Users.Update(user);
        }

        if (user.ActiveCommandParams["ip"] != String.Empty && user.ActiveCommandParams["port"] != String.Empty)
        {
            if (Extensions.IsValidFluxPortString(user.ActiveCommandParams["port"]) 
                && Extensions.IsValidIPString(user.ActiveCommandParams["ip"]))
            {
                var newNode = new Node
                {
                    Id = Guid.NewGuid().ToString(),
                    IP = user.ActiveCommandParams["ip"],
                    Port = Convert.ToInt32(user.ActiveCommandParams["port"])
                };
                user.Nodes.Add(newNode);
                Database.DefaultInstance.Users.Update(user);
                _telegramClient.SendMessage(chatId, $"node {newNode} added. At any time you ask me about your nodes status with _/status_ command");
            }
            else
            {
                _telegramClient.SendMessage(chatId, "IP & port don't seem valid to me. Please start again with _/addnode_ command");
            }
            
            UserService.ResetActiveCommand(user);
        }
    }

    public void HandleStatusCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null)
        {
            return;
        }

        if (user.Nodes.Count == 0)
        {
            _telegramClient.SendMessage(chatId, "I don't know any of your nodes yet, please add with _/addnode_ command");
            return;
        }
        
        var builder = new StringBuilder();
        builder.AppendLine("* Nodes Status *");
        builder.AppendLine();

        foreach (var node in user.Nodes)
        {
            builder.AppendLine($"*{node.ToString()}*");
            builder.AppendLine($"status: *{node.LastStatus}*");
            builder.AppendLine($"_checked at {node.LastCheckDateTime} UTC_");
            builder.AppendLine($"rank: *{node.Rank}*");
            builder.AppendLine($"_next payment in {TimeSpan.FromMinutes(node.Rank * 2).ToReadableString()}_");
            builder.AppendLine();
        }

        builder.AppendLine($"{DateTime.UtcNow} UTC");
        _telegramClient.SendMessage(chatId, builder.ToString(), message.MessageId);
    }

    public void HandleMyNodesCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null)
        {
            return;
        }

        var result = $"you have {user.Nodes.Count} nodes"+ Environment.NewLine;
        foreach (var node in user.Nodes)
        {
            result += $"{node}" + Environment.NewLine;
        }
        
        _telegramClient.SendMessage(chatId, result, message.MessageId);
    }

    public void HandleRemoveAllNodesCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null)
        {
            return;
        }

        UserService.RemoveAllNodes(user);
        var result = $"all nodes removed";
        
        _telegramClient.SendMessage(chatId, result, message.MessageId);
    }

    private void HandleAdminUsersCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (chatId != Program.FluxConfig.MChatId)
        {
            return;
        }
        if (user is null)
        {
            return;
        }

        var allUsers = Database.DefaultInstance.Users.FindAll().ToList();
        var result = new StringBuilder();
        result.AppendLine($"there are {allUsers.Count} users");
        result.AppendLine();
        foreach (var aUser in allUsers)
        {
            result.AppendLine($"{aUser.TelegramUsername}:{aUser.TelegramChatId}:{aUser.ActiveCommand}");
            foreach (var userNode in aUser.Nodes)
            {
                result.AppendLine($"{userNode.IP}:{userNode.Port}");
            }

            result.AppendLine();
        }

        _telegramClient.SendMessage(chatId, result.ToString(), message.MessageId);
    }
    
}