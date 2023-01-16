using Discord;
using Discord.WebSocket;
using FluxGuardian.Data;

namespace FluxGuardian.Services.Discord;

public class DiscordService
{
    private readonly DiscordSocketClient _client;

    public DiscordService(string token)
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
            { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent });
        _client.MessageReceived += ClientOnMessageReceived;
        _client.Log += message => Task.Run(() => Console.WriteLine($"log: {message}"));
        _client.LoginAsync(TokenType.Bot, token);
    }

    public void Start()
    {
        _client.StartAsync();
    }
    
    private Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (arg is not SocketUserMessage msg)
            return Task.CompletedTask;

        // We don't want the bot to respond to itself or other bots.
        if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot)
            return Task.CompletedTask;

        Console.WriteLine($"message: {msg}");
        var command = msg.Content;

        var user = FindUser(msg.Author.Username, (long)msg.Author.Id);
        if (command.StartsWith("/"))
        {
            HandleCommands(msg, command);
        }
        else if(user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
        {
            HandleCommands(msg, user.ActiveCommand);
        }
        
        return Task.CompletedTask;
    }
    
    private void HandleCommands(SocketUserMessage message, string command)
    {
        switch (command)
        {
            case "":
                break;
            case "/start":
                HandleStartCommand(message);
                break;
        }
    }

    private void HandleStartCommand(SocketUserMessage message)
    {
        var chatId = (long) message.Author.Id;
        var username = message.Author.Username;
        var user = FindUser(username, chatId);
        if (user is not null)
        {
            ResetActiveCommand(user);
            RemoveAllNodes(user);
        }
        
        var text = @"Hellow from FluxGuardian bot 🤖 

This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you. 

Currently, you can add up to 2 nodes. 

This bot is in Beta and is available ""AS IS"" without any warranty of any kind.";
        
        message.Channel.SendMessageAsync(text, messageReference: new MessageReference(message.Id));
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

    private static Models.User? FindUser(string username, long id)
    {
        var user = Database.Users.FindOne(user => user.DiscordId.Equals(id));
        return user;
    }

    private static Models.User CreateUser(string username, long id)
    {
        var user = FindUser(username, id);
        if (user is null)
        {
            user = new Models.User
            {
                DiscordUsername = username,
                DiscordId = id,
            };
            Database.Users.Insert(user);
        }

        return user;
    }
}