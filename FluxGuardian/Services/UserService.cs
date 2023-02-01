using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;

namespace FluxGuardian.Services;

public static class UserService
{
    public static void ResetActiveCommand(User user)
    {
        user.ActiveCommand = null;
        user.ActiveCommandParams.Clear();
        Database.DefaultInstance.Users.Update(user);
    }

    public static void RemoveAllNodes(User user)
    {
        user.Nodes.Clear();
        Database.DefaultInstance.Users.Update(user);
    }

    public static User? FindOrCreateUser(CommandContext context)
    {
        var user = FindUser(context);
        if (user is null)
        {
            if (context.ContextKind == ContextKind.Telegram)
            {
                user = CreateTelegramUser(context.Username, Convert.ToInt64(context.UserId));
            }

            if (context.ContextKind == ContextKind.Discord)
            {
                user = CreateDiscordUser(context.Username, Convert.ToUInt64(context.UserId));
            }
        }

        return user;
    }

    public static User? FindUser(CommandContext context)
    {
        if (context.ContextKind == ContextKind.Telegram)
            return FindTelegramUser(Convert.ToInt64(context.UserId));
        if (context.ContextKind == ContextKind.Discord)
            return FindDiscordUser(Convert.ToUInt64(context.UserId));

        return null;
    }

    public static User? FindDiscordUser(ulong id)
    {
        try
        {
            var user = Database.DefaultInstance.Users.FindOne(user => user.DiscordId == id);
            return user;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static User? FindTelegramUser(long id)
    {
        var user = Database.DefaultInstance.Users.FindOne(user => user.TelegramChatId == id);
        return user;
    }

    public static User CreateTelegramUser(string username, long id)
    {
        var user = FindTelegramUser(id);
        if (user is null)
        {
            user = new User
            {
                TelegramUsername = username,
                TelegramChatId = id,
            };
            Database.DefaultInstance.Users.Insert(user);
        }

        return user;
    }

    public static User CreateDiscordUser(string username, ulong id)
    {
        var user = FindDiscordUser(id);
        if (user is null)
        {
            user = new User
            {
                DiscordUsername = username,
                DiscordId = id,
            };
            Database.DefaultInstance.Users.Insert(user);
        }

        return user;
    }

    public static Node AddNode(User user, string ip, int port)
    {
        if (user.Nodes.Count >= Constants.MaximumNodeCount)
        {
            throw new Exception("sorry, you have reached the maximum number of nodes");
        }

        var newNode = new Node
        {
            Id = Guid.NewGuid().ToString(),
            IP = ip,
            Port = port
        };
        user.Nodes.Add(newNode);
        Database.DefaultInstance.Users.Update(user);
        return newNode;
    }
}