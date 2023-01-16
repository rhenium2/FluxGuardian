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
        
        var user = FindUser(message);
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
        }
    }
    
    public void HandleStartCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message);
        if (user is not null)
        {
            ResetActiveCommand(user);
            RemoveAllNodes(user);
        }
        
        var text = @"Hellow from FluxGuardian bot 🤖 

This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you. 

Currently, you can add up to 2 nodes. 

This bot is in Beta and is available ""AS IS"" without any warranty of any kind.";

        _telegramClient.SendMessage(chatId, text, message.MessageId);
    }

    public void HandleAddNodeCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message) ?? CreateUser(message);

        if (user.Nodes.Count >= 2)
        {
            _telegramClient.SendMessage(chatId, "sorry, you have reached the maximum number of nodes");
            return;
        }
        
        if (string.IsNullOrEmpty(user.ActiveCommand))
        {
            user.ActiveCommand = message.Text;
            Database.Users.Update(user);
        }

        if (!user.ActiveCommandParams.ContainsKey("ip"))
        {
            user.ActiveCommandParams["ip"] = String.Empty;
            Database.Users.Update(user);
            _telegramClient.SendMessage(chatId, "sure, what is this node IP address?");
        }
        else if (string.IsNullOrEmpty(user.ActiveCommandParams["ip"]))
        {
            // TODO: validate ip address here
            user.ActiveCommandParams["ip"] = message.Text.Trim();
            Database.Users.Update(user);
        }
        
        if (!string.IsNullOrEmpty(user.ActiveCommandParams["ip"]) && !user.ActiveCommandParams.ContainsKey("port"))
        {
            user.ActiveCommandParams["port"] = String.Empty;
            Database.Users.Update(user);
            _telegramClient.SendMessage(chatId, "what is this node's API port? (usually 16127)");
        }
        else if (!string.IsNullOrEmpty(user.ActiveCommandParams["ip"]) && string.IsNullOrEmpty(user.ActiveCommandParams["port"]))
        {
            // TODO: validate port address here
            user.ActiveCommandParams["port"] = message.Text.Trim();
            Database.Users.Update(user);
        }

        if (user.ActiveCommandParams["ip"] != String.Empty && user.ActiveCommandParams["port"] != String.Empty)
        {
            user.Nodes.Add(new Node
            {
                Id = Guid.NewGuid().ToString(),
                IP = user.ActiveCommandParams["ip"]!,
                Port = Convert.ToInt32(user.ActiveCommandParams["port"]!)
            });
            
            ResetActiveCommand(user);

            _telegramClient.SendMessage(chatId, "node added");
        }
    }

    public void HandleStatusCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message);
        if (user is null)
        {
            return;
        }

        if (user.Nodes.Count == 0)
        {
            return;
        }
        
        var builder = new StringBuilder();
        builder.AppendLine("* Nodes Status *");
        builder.AppendLine();

        foreach (var node in user.Nodes)
        {
            builder.AppendLine($"*{node.ToString()}*");
            builder.AppendLine($"rank: *{node.Rank}*");
            builder.AppendLine($"status: *{node.LastStatus}*");
            builder.AppendLine($"_checked at {node.LastCheckDateTime} UTC_");
            builder.AppendLine();
        }

        builder.AppendLine("current UTC time: " + DateTime.UtcNow);
        
        _telegramClient.SendMessage(chatId, builder.ToString(), message.MessageId);
    }

    public void HandleMyNodesCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message);
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
        var user = FindUser(message);
        if (user is null)
        {
            return;
        }

        RemoveAllNodes(user);
        var result = $"all nodes removed";
        
        _telegramClient.SendMessage(chatId, result, message.MessageId);
    }

    private void HandleAdminUsersCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message);
        if (chatId != Program.FluxConfig.MChatId)
        {
            return;
        }
        if (user is null)
        {
            return;
        }

        var allUsers = Database.Users.FindAll().ToList();
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

    private static void ResetActiveCommand(Models.User user)
    {
        user.ActiveCommand = null;
        user.ActiveCommandParams.Clear();
        Database.Users.Update(user);
    }

    private static void RemoveAllNodes(Models.User user)
    {
        user.Nodes.Clear();
        Database.Users.Update(user);
    }

    private static Models.User? FindUser(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username;
        var user = Database.Users.FindOne(user => user.TelegramChatId.Equals(chatId));
        return user;
    }

    private static Models.User CreateUser(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username;
        var user = Database.Users.FindOne(user => user.TelegramChatId.Equals(chatId));
        if (user is null)
        {
            user = new Models.User
            {
                TelegramUsername = username,
                TelegramChatId = chatId,
            };
            Database.Users.Insert(user);
        }

        return user;
    }

}