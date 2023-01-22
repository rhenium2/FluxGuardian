using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FluxGuardian.Services.Discord;

public class DiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;

    public DiscordService(string token)
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
            { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent });
        _client.MessageReceived += ClientOnMessageReceived;
        _client.Log += message => Task.Run(() => Console.WriteLine($"log: {message}"));
        _client.LoginAsync(TokenType.Bot, token);

        _commandService = new CommandService();
        _commandService.AddModuleAsync<CommandModule>(null).Wait();
    }

    public void Start()
    {
        _client.StartAsync();
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        // Don't process the command if it was a system message
        var message = arg as SocketUserMessage;
        if (message == null) return;

        Console.WriteLine($"message: {message}");
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.HasCharPrefix('!', ref argPos) ||
              message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
            return;

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(_client, message);

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        await _commandService.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: null);

        /*var command = msg.Content;

        var user = UserService.FindDiscordUser((long)msg.Author.Id);
        if (command.StartsWith("/"))
        {
            HandleCommands(msg, command);
        }
        else if(user != null && !string.IsNullOrWhiteSpace(user.ActiveCommand))
        {
            HandleCommands(msg, user.ActiveCommand);
        }
        
        return Task.CompletedTask;*/
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
        var chatId = (long)message.Author.Id;
        var username = message.Author.Username;
        var user = UserService.FindDiscordUser(chatId);
        if (user is not null)
        {
            UserService.ResetActiveCommand(user);
            UserService.RemoveAllNodes(user);
        }

        var text = @"Hellow from FluxGuardian bot 🤖 

This bot checks your flux nodes regularly to make sure they are up, reachable and confirmed. Otherwise it will send a message to you and notifies you. 

Currently, you can add up to 2 nodes. 

This bot is in Beta and is available ""AS IS"" without any warranty of any kind.";

        message.Channel.SendMessageAsync(text, messageReference: new MessageReference(message.Id));
    }
}