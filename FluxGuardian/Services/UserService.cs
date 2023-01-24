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

    public static User? FindDiscordUser(ulong id)
    {
        var user = Database.DefaultInstance.Users.FindOne(user => user.DiscordId.Equals(id));
        return user;
    }

    public static User? FindTelegramUser(long id)
    {
        var user = Database.DefaultInstance.Users.FindOne(user => user.TelegramChatId.Equals(id));
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