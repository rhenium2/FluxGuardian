using System.Text;
using FluxGuardian.Data;
using FluxGuardian.Helpers;
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

        var user = UserService.FindTelegramUser(chatId);
        if (command.StartsWith("/"))
            HandleCommands(message, command);
        else if (user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
            HandleCommands(message, user.ActiveCommand);
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
            case string when command.StartsWith("/admin/broadcastmessage"):
                HandleAdminBroadcastMessageCommand(message);
                break;
        }
    }

    private void HandleAdminBroadcastMessageCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null) return;

        if (chatId != Program.FluxConfig.TelegramBotConfig.ModeratorChatId) return;

        var commandItems = message.Text.Trim()
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var sendingMessage = string.Join(' ', commandItems.Skip(1));
        var users = Database.DefaultInstance.Users.Find(x => x.TelegramChatId > 0).ToList();

        foreach (var sendingUser in users)
        {
            _telegramClient.SendMessage(sendingUser.TelegramChatId, sendingMessage);
        }

        _telegramClient.SendMessage(chatId, $"message \"{sendingMessage}\" sent to {users.Count} users");
    }

    private void HandleAdminSendMessageCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null) return;

        if (chatId != Program.FluxConfig.TelegramBotConfig.ModeratorChatId) return;

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
        if (chatId != Program.FluxConfig.TelegramBotConfig.ModeratorChatId) return;

        if (user is null) return;

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
        text.AppendLine(
            "This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you.");
        text.AppendLine();
        text.AppendLine($"Currently, you can add up to {Constants.MaximumNodeCount} nodes.");
        text.AppendLine();
        text.AppendLine("This bot is in Beta and is available \"AS IS\" without any warranty of any kind.");
        text.AppendLine();
        text.AppendLine(
            "type /addnode to add a new node for me to monitor. At any point if you are stuck, type /start and you can start fresh.");

        _telegramClient.SendMessage(chatId, text.ToString(), message.MessageId);
    }

    public void HandleAddNodeCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId) ?? UserService.CreateTelegramUser(username, chatId);

        if (user.Nodes.Count >= Constants.MaximumNodeCount)
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
            user.ActiveCommandParams["ip"] = string.Empty;
            Database.DefaultInstance.Users.Update(user);
            _telegramClient.SendMessage(chatId, "sure, what is this node IP address? (example: 123.123.123.123)");
        }
        else if (string.IsNullOrEmpty(user.ActiveCommandParams["ip"]))
        {
            user.ActiveCommandParams["ip"] = message.Text.Trim();
            Database.DefaultInstance.Users.Update(user);
        }


        var nodeIp = user.ActiveCommandParams["ip"];
        if (!string.IsNullOrWhiteSpace(nodeIp))
        {
            if (Extensions.IsValidIPString(nodeIp))
            {
                var activePortSets = NodeService.FindActivePortSets(nodeIp);
                if (!activePortSets.Any())
                {
                    _telegramClient.SendMessage(chatId,
                        message: "I Couldn't find any nodes on any ports.\n" +
                                 "I checked all these port ranges: \n" +
                                 $"{string.Join("\n", Constants.FluxPortSets.Values)}" +
                                 "\n" +
                                 "Please start again with _/addnode_ command"
                    );
                }
                else
                {
                    foreach (var activePortSet in activePortSets)
                    {
                        var newNode = UserService.AddNode(user, nodeIp, activePortSet.Key);

                        _telegramClient.SendMessage(chatId,
                            message: "Success!\n" +
                                     $"Found a Flux node on ports {activePortSet}\n" +
                                     $"I saved {newNode.ToIPAndPortText()} successfully.\n" +
                                     "At any time you can ask me about all your nodes status with _/status_ command");
                    }

                    NodeGuard.CheckUserNodes(user);
                }
            }
            else
            {
                _telegramClient.SendMessage(chatId,
                    "IP doesn't seem valid to me. Please start again with _/addnode_ command");
            }

            UserService.ResetActiveCommand(user);
        }
    }

    public void HandleStatusCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var username = message.From.Username;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null) return;

        if (user.Nodes.Count == 0)
        {
            _telegramClient.SendMessage(chatId,
                "I don't know any of your nodes yet, please add with _/addnode_ command");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("* Nodes Status *");
        builder.AppendLine();

        foreach (var node in user.Nodes)
        {
            var nodePortSet = Constants.FluxPortSets[node.Port];
            var nodeIcon = node.LastStatus == "CONFIRMED" && !node.ClosedPorts.Any() ? "🟢" : "🔴";
            builder.AppendLine($"{nodeIcon} *{node.ToIPAndPortText()}*");
            builder.AppendLine($"status: *{node.LastStatus}* ({node.LastCheckDateTime?.ToRelativeText()})");
            if (node.ClosedPorts.Any())
            {
                builder.AppendLine($"ports: {string.Join(", ", node.ClosedPorts)} are closed");
            }
            else
            {
                builder.AppendLine($"ports: {nodePortSet} are open");
            }

            builder.AppendLine(
                $"rank: *{node.Rank}* (_payment in {TimeSpan.FromMinutes(node.Rank * 2).ToReadableString()}_)");
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
        if (user is null) return;

        var result = $"you have {user.Nodes.Count} nodes" + Environment.NewLine;
        foreach (var node in user.Nodes) result += $"{node}" + Environment.NewLine;

        _telegramClient.SendMessage(chatId, result, message.MessageId);
    }

    public void HandleRemoveAllNodesCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (user is null) return;

        UserService.RemoveAllNodes(user);
        var result = $"all nodes removed";

        _telegramClient.SendMessage(chatId, result, message.MessageId);
    }

    private void HandleAdminUsersCommand(Message message)
    {
        var chatId = message.Chat.Id;
        var user = UserService.FindTelegramUser(chatId);
        if (chatId != Program.FluxConfig.TelegramBotConfig.ModeratorChatId) return;

        if (user is null) return;

        var allUsers = Database.DefaultInstance.Users.FindAll().ToList();
        var result = new StringBuilder();
        result.AppendLine($"there are {allUsers.Count} users");
        result.AppendLine();
        foreach (var aUser in allUsers)
        {
            result.AppendLine($"{aUser.ToString()}:{aUser.ActiveCommand}");
            foreach (var userNode in aUser.Nodes)
            {
                result.AppendLine($"{userNode.ToIPAndPortText()}");
                result.AppendLine($"{userNode.LastStatus}");
                result.AppendLine($"{userNode.Tier}");
            }

            result.AppendLine();
        }

        _telegramClient.SendMessage(chatId, result.ToString(), message.MessageId);
    }
}