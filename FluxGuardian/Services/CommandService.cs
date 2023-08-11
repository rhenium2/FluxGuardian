using System.Reflection;
using System.Text;
using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;

namespace FluxGuardian.Services;

public static class CommandService
{
    [CommandDescription(Name = "start", Description = "starts a new session and shows the starting message")]
    public static void HandleStartCommand(CommandContext context)
    {
        var chatId = context.UserId;
        var username = context.Username;
        var user = UserService.FindUser(context);
        if (user is not null)
        {
            UserService.ResetActiveCommand(user);
            UserService.RemoveAllNodes(user);
        }

        var text = new StringBuilder();
        text.AppendLine("👋 Hellow from FluxGuardian bot 🤖");
        text.AppendLine(
            "This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you.");
        text.AppendLine("This bot is free of charge and is available \"AS IS\" without any warranty of any kind.");
        text.AppendLine();
        text.AppendLine(
            $"Type {CommandNotation(context, "addnode")} to add a new node for me to monitor. At any point if you are stuck, type {CommandNotation(context, "start")} and you can start fresh.");
        text.AppendLine($"Type {CommandNotation(context, "help")} to show all supported commands");
        text.AppendLine($"Currently, you can add up to {Constants.MaximumNodeCount} nodes");
        text.AppendLine();
        text.AppendLine($"Support: {Constants.SupportUrl}");

        SendMessage(context, text.ToString());
    }

    [CommandDescription(Name = "addnode", Description = "adds a new Flux node for me to watch")]
    [CommandExample(ContextKind = ContextKind.Discord, Example = "!addnode 123.123.123.123")]
    public static void HandleAddNodeCommand(CommandContext context)
    {
        var user = UserService.FindOrCreateUser(context);

        if (user.Nodes.Count >= Constants.MaximumNodeCount)
        {
            SendMessage(context, "sorry, you have reached the maximum number of nodes");
            return;
        }

        if (context.Arguments.ContainsKey("ip"))
        {
            user.ActiveCommandParams["ip"] = context.Arguments["ip"].Trim();
            Database.DefaultInstance.Users.Update(user);
        }

        if (string.IsNullOrEmpty(user.ActiveCommand))
        {
            user.ActiveCommand = context.Message;
            Database.DefaultInstance.Users.Update(user);
        }

        if (!user.ActiveCommandParams.ContainsKey("ip"))
        {
            user.ActiveCommandParams["ip"] = string.Empty;
            Database.DefaultInstance.Users.Update(user);
            SendMessage(context,
                "sure, what is this node public IP address? (example: 123.123.123.123)");
        }
        else if (string.IsNullOrEmpty(user.ActiveCommandParams["ip"]))
        {
            user.ActiveCommandParams["ip"] = context.Message.Trim();
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
                    SendMessage(context,
                        message: "I Couldn't find any nodes on any ports.\n" +
                                 "I checked all these port ranges: \n" +
                                 $"{string.Join("\n", Constants.FluxPortSets.Values)}" +
                                 "\n" +
                                 $"Please start again with _{CommandNotation(context, "addnode")}_ command"
                    );
                }
                else
                {
                    var newPortSets = activePortSets.Except(NodeService.GetUserNodePortSets(user));
                    if (!newPortSets.Any())
                    {
                        SendMessage(context,
                            $"I am already watching {activePortSets.Count} nodes on this IP. No new nodes was found");
                    }
                    else
                    {
                        foreach (var activePortSet in newPortSets)
                        {
                            try
                            {
                                var newNode = UserService.AddNode(user, nodeIp, activePortSet.Key);

                                SendMessage(context,
                                    message: "Success!\n" +
                                             $"Found a Flux node on ports {activePortSet}\n" +
                                             $"I saved {newNode.ToIPAndPortText()} successfully.\n" +
                                             $"At any time you can ask me about all your nodes status with _{CommandNotation(context, "status")}_ command");
                            }
                            catch (Exception e)
                            {
                                Logger.LogOutput($"Received exception {e.ToString()}");
                                SendMessage(context,
                                    message: $"I could NOT save Flux node on ports {activePortSet}\n" +
                                             $"Most common reason is you have reached the maximum number of nodes, which is currently {Constants.MaximumNodeCount} nodes.\n");
                            }
                        }

                        NodeChecker.CheckUserNodes(user);
                    }
                }
            }
            else
            {
                SendMessage(context,
                    $"IP doesn't seem valid to me. Make sure you used the public IP address of the node and do not include the port. Please start again with _{CommandNotation(context, "addnode")}_ command");
            }

            UserService.ResetActiveCommand(user);
        }
    }

    [CommandDescription(Name = "status", Description = "shows the last status of all your Flux nodes")]
    public static void HandleStatusCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);
        if (user is null)
        {
            return;
        }
        else
        {
            UserService.ResetActiveCommand(user);
        }

        if (user.Nodes.Count == 0)
        {
            SendMessage(context,
                $"I don't know any of your nodes yet, please add with _{CommandNotation(context, "addnode")}_ command");
            return;
        }


        var builder = new StringBuilder();
        builder.AppendLine("* Nodes Status *");
        builder.AppendLine();

        foreach (var node in user.Nodes)
        {
            var nodeDisabledText = NodeChecker.ShouldSkipNodeCheck(node) ? $"(disabled temporarily)" : "";
            var nodePortSet = Constants.FluxPortSets[node.Port];
            var nodeIcon = node.LastStatus == NodeStatus.Confirmed && !node.ClosedPorts.Any() ? "🟢" : "🔴";
            builder.AppendLine($"{nodeIcon} *{node.ToIPAndPortText()}*");
            builder.AppendLine(
                $"status: *{node.LastStatus}* ({node.LastCheckDateTime?.ToRelativeText()}) {nodeDisabledText}");
            builder.AppendLine($"version: v{node.FluxVersion}");
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

        builder.AppendLine("FluxGuardian bot 🤖");
        builder.AppendLine($"Support: {Constants.SupportUrl}");
        SendMessage(context, builder.ToString());
    }

    [CommandDescription(Name = "mynodes", Description = "shows all nodes added to the bot")]
    public static void HandleMyNodesCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);
        if (user is null)
        {
            return;
        }
        else
        {
            UserService.ResetActiveCommand(user);
        }

        var builder = new StringBuilder();
        builder.AppendLine($"* you have {user.Nodes.Count} nodes *");
        builder.AppendLine();
        foreach (var node in user.Nodes)
        {
            builder.AppendLine($"{node}");
        }

        SendMessage(context, builder.ToString());
    }

    [CommandDescription(Name = "removeallnodes", Description = "removes all nodes added to the bot")]
    public static void HandleRemoveAllNodesCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);
        if (user is null)
        {
            return;
        }

        UserService.RemoveAllNodes(user);
        var result = $"all nodes removed";

        SendMessage(context, result);
    }

    [CommandDescription(Name = "help", Description = "shows this help message")]
    public static void HandleHelpCommand(CommandContext context)
    {
        var methodsWithAttributes = typeof(CommandService).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.CustomAttributes.Any());

        var output = new StringBuilder();

        output.AppendLine("The commands I support are:");
        foreach (var methodInfo in methodsWithAttributes)
        {
            var descriptionAttribute = methodInfo.GetCustomAttributes<CommandDescriptionAttribute>().FirstOrDefault();
            var commandExamples = methodInfo.GetCustomAttributes<CommandExampleAttribute>()
                .Where(x => x.ContextKind == context.ContextKind).ToList();

            if (descriptionAttribute is null)
            {
                continue;
            }

            output.AppendLine(
                $"{CommandNotation(context, descriptionAttribute.Name)} - {descriptionAttribute.Description}");

            foreach (var commandExample in commandExamples)
            {
                output.AppendLine($"   e.g. {commandExample.Example}");
            }
        }

        output.AppendLine();
        output.AppendLine("FluxGuardian bot 🤖");
        SendMessage(context, output.ToString());
    }

    #region Admin commands

    public static void HandleAdminUsersCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);
        if (!IsModeratorContext(context))
        {
            return;
        }

        if (user is null)
        {
            return;
        }

        var allUsers = Database.DefaultInstance.Users.FindAll().ToList();
        var result = new StringBuilder();
        var nodesCount = 0;
        foreach (var aUser in allUsers)
        {
            result.AppendLine($"{aUser.GetUserContextKind()}:{aUser.ToString()}:{aUser.ActiveCommand}");
            foreach (var userNode in aUser.Nodes)
            {
                result.AppendLine($"{userNode.ToIPAndPortText()}");
                result.AppendLine($"{userNode.LastStatus}");
                result.AppendLine($"{userNode.Tier}");
                nodesCount++;
            }

            result.AppendLine();
        }

        result.AppendLine();
        result.AppendLine($"there are {allUsers.Count} users and {nodesCount} nodes");

        SendMessage(context, result.ToString());
    }

    public static void HandleAdminBroadcastMessageCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);

        if (!IsModeratorContext(context))
        {
            return;
        }

        if (user is null)
        {
            return;
        }

        // TODO: support other channels
        if (context.ContextKind != ContextKind.Telegram)
        {
            SendMessage(context, $"only telegram is supported for this command");
            return;
        }

        var commandItems = context.Message.Trim()
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var sendingMessage = string.Join(' ', commandItems.Skip(1));
        var users = Database.DefaultInstance.Users.Find(x => x.TelegramChatId > 0).ToList();

        foreach (var sendingUser in users)
        {
            var sendingContext = new CommandContext
            {
                UserId = sendingUser.TelegramChatId.ToString(),
                ContextKind = ContextKind.Telegram
            };
            SendMessage(sendingContext, sendingMessage);
        }

        SendMessage(context, $"message \"{sendingMessage}\" sent to {users.Count} users");
    }

    public static void HandleAdminSendMessageCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);

        if (!IsModeratorContext(context))
        {
            return;
        }

        if (user is null)
        {
            return;
        }

        // TODO: support other channels
        if (context.ContextKind != ContextKind.Telegram)
        {
            SendMessage(context, $"only telegram is supported for this command");
            return;
        }

        var commandItems = context.Message.Trim()
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var sendingChatId = Convert.ToInt64(commandItems[1].Trim());
        var sendingMessage = string.Join(' ', commandItems.Skip(2));
        var sendingUser = UserService.FindTelegramUser(sendingChatId);
        if (sendingUser is not null)
        {
            var sendingContext = new CommandContext
            {
                UserId = sendingUser.TelegramChatId.ToString(),
                ContextKind = ContextKind.Telegram
            };
            SendMessage(sendingContext, sendingMessage);
            SendMessage(context, $"message \"{sendingMessage}\" sent to {sendingChatId}");
        }
    }

    public static void HandleAdminDeleteUserCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);

        if (!IsModeratorContext(context))
        {
            return;
        }

        if (user is null)
        {
            return;
        }

        var deletingChatId = Convert.ToInt64(context.Message.Split(' ')[1].Trim());
        var deletingUser = UserService.FindTelegramUser(deletingChatId);
        if (deletingUser is not null)
        {
            Database.DefaultInstance.Users.Delete(deletingUser.Id);
            SendMessage(context, "user deleted");
        }
    }

    public static void HandleAdminAddNodeCommand(CommandContext context)
    {
        var user = UserService.FindUser(context);

        if (!IsModeratorContext(context))
        {
            return;
        }

        if (user is null)
        {
            return;
        }

        var ip = (context.Message.Split(' ')[1].Trim());
        var port = Convert.ToInt32(context.Message.Split(' ')[2].Trim());
        UserService.AddNode(user, ip, port);
        SendMessage(context, "node added.");
    }

    #endregion

    private static void SendMessage(CommandContext context, string message)
    {
        Notifier.NotifyContext(context, message);
    }

    private static bool IsModeratorContext(CommandContext context)
    {
        if (context.ContextKind == ContextKind.Telegram)
        {
            return context.UserId == Program.FluxConfig.TelegramBotConfig.ModeratorChatId.ToString();
        }

        if (context.ContextKind == ContextKind.Discord)
        {
            return false;
        }

        return false;
    }

    private static string CommandNotation(CommandContext context, string command)
    {
        if (context.ContextKind == ContextKind.Telegram)
            return $"/{command}";

        if (context.ContextKind == ContextKind.Discord)
            return $"!{command}";

        return command;
    }
}