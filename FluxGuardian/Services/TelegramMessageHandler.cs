using System.Text;
using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FluxGuardian.Models.User;

namespace FluxGuardian.Services;

public class TelegramMessageHandler : IUpdateHandler, IMessengerCommands
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
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
        var user = FindUser(message);
        Logger.Log($"Received '{messageText}' from {message.From.Username}:{chatId}");
        Logger.LogMessage($"{message.From.Username}:{message.Chat.Id} {messageText}");
        
        if (command.StartsWith("/"))
        {
            await HandleCommands(botClient, message, command);
        }
        else if(user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
        {
            await HandleCommands(botClient, message, user.ActiveCommand);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Logger.Log($"Received exception {exception.ToString()}");
        return Task.CompletedTask;
    }

    private async Task HandleCommands(ITelegramBotClient botClient, Message message, string command)
    {
        switch (command)
        {
            case "":
                break;
            case "/start":
                await HandleStartCommand(botClient, message);
                break;
            case "/addnode":
                await HandleAddNodeCommand(botClient, message);
                break;
            case "/status":
                await HandleStatusCommand(botClient, message);
                break;
            case "/mynodes":
                await HandleMyNodesCommand(botClient, message);
                break;
            case "/removeallnodes":
                await HandleRemoveAllNodesCommand(botClient, message);
                break;
            case "/admin/users":
                await HandleAdminUsersCommand(botClient, message);
                break;
        }
    }

    private async Task HandleAdminUsersCommand(ITelegramBotClient botClient, Message message)
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

        await SendMessage(botClient, chatId, result.ToString(), message.MessageId);
    }

    public async Task HandleStartCommand(ITelegramBotClient botClient, Message message)
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
        
        await SendMessage(botClient, chatId, text, message.MessageId);
    }

    public async Task HandleMyNodesCommand(ITelegramBotClient botClient, Message message)
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
        
        await SendMessage(botClient, chatId, result, message.MessageId);
    }

    public async Task HandleRemoveAllNodesCommand(ITelegramBotClient botClient, Message message)
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
        
        await SendMessage(botClient, chatId, result, message.MessageId);
    }

    public async Task HandleStatusCommand(ITelegramBotClient botClient, Message message)
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
        
        // TODO: change to markdown for better read
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
        
        await SendMessage(botClient, chatId, builder.ToString(), message.MessageId);
    }

    public async Task HandleAddNodeCommand(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = FindUser(message) ?? CreateUser(message);

        if (user.Nodes.Count >= 2)
        {
            await SendMessage(botClient, chatId, "sorry, you have reached the maximum number of nodes");
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
            await SendMessage(botClient, chatId, "sure, what is this node IP address?");
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
            await SendMessage(botClient, chatId, "what is this node's API port? (usually 16127)");
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

            await SendMessage(botClient, chatId, "node added");
        }
    }

    private static void ResetActiveCommand(User user)
    {
        user.ActiveCommand = null;
        user.ActiveCommandParams.Clear();
        Database.Users.Update(user);
    }

    private static void RemoveAllNodes(User user)
    {
        user.Nodes.Clear();
        Database.Users.Update(user);
    }

    private static User? FindUser(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username;
        var user = Database.Users.FindOne(user => user.TelegramChatId.Equals(chatId));
        return user;
    }

    private static User CreateUser(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username;
        var user = Database.Users.FindOne(user => user.TelegramChatId.Equals(chatId));
        if (user is null)
        {
            user = new User
            {
                TelegramUsername = username,
                TelegramChatId = chatId,
            };
            Database.Users.Insert(user);
        }

        return user;
    }

    private async Task SendMessage(ITelegramBotClient botClient, long chatId, string message, int? replyToMessageId = null)
    {
        try
        {
            await botClient.SendTextMessageAsync(chatId, message, replyToMessageId: replyToMessageId, parseMode: ParseMode.Markdown);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log($"Received exception {e}");
        }
    }
}

public interface IMessengerCommands
{
    Task HandleStartCommand(ITelegramBotClient botClient, Message message);
    Task HandleMyNodesCommand(ITelegramBotClient botClient, Message message);
    Task HandleRemoveAllNodesCommand(ITelegramBotClient botClient, Message message);
    Task HandleStatusCommand(ITelegramBotClient botClient, Message message);
    Task HandleAddNodeCommand(ITelegramBotClient botClient, Message message);
}