using FluxGuardian.Data;

namespace FluxGuardian.Services;

public static class UserService
{
    public static void ResetActiveCommand(Models.User user)
    {
        user.ActiveCommand = null;
        user.ActiveCommandParams.Clear();
        Database.DefaultInstance.Users.Update(user);
    }

    public static void RemoveAllNodes(Models.User user)
    {
        user.Nodes.Clear();
        Database.DefaultInstance.Users.Update(user);
    }

    public static Models.User? FindDiscordUser(long id)
    {
        var user = Database.DefaultInstance.Users.FindOne(user => user.DiscordId.Equals(id));
        return user;
    }
    
    public static Models.User? FindTelegramUser(long id)
    {
        var user = Database.DefaultInstance.Users.FindOne(user => user.TelegramChatId.Equals(id));
        return user;
    }

    public static Models.User CreateTelegramUser(string username, long id)
    {
        var user = FindTelegramUser(id);
        if (user is null)
        {
            user = new Models.User
            {
                TelegramUsername = username,
                TelegramChatId = id,
            };
            Database.DefaultInstance.Users.Insert(user);
        }

        return user;
    }
    
    public static Models.User CreateDiscordUser(string username, long id)
    {
        var user = FindTelegramUser(id);
        if (user is null)
        {
            user = new Models.User
            {
                DiscordUsername = username,
                DiscordId = id,
            };
            Database.DefaultInstance.Users.Insert(user);
        }

        return user;
    }
}