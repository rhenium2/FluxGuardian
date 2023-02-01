using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluxGuardian.Helpers;

namespace FluxGuardian.Services.Discord;

public class DiscordClient
{
    private readonly string _token;
    private readonly DiscordSocketClient _client;
    private readonly global::Discord.Commands.CommandService _discordCommandService;

    public DiscordClient(string token)
    {
        _token = token;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent |
                             GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        });


        _discordCommandService = new global::Discord.Commands.CommandService();
        _discordCommandService.AddModuleAsync<DiscordCommandModule>(null).Wait();
    }

    public void Init()
    {
        _client.MessageReceived += ClientOnMessageReceived;
        //_client.Log += message => Task.Run(() => Console.WriteLine($"log: {message}"));
        _client.LoginAsync(TokenType.Bot, _token);
        _client.StartAsync();
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        // Don't process the command if it was a system message
        var message = arg as SocketUserMessage;
        if (message == null)
        {
            return;
        }

        Console.WriteLine($"message: {message}");
        // Create a number to track where the prefix ends and the command begins
        var argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (message.Author.IsBot)
        {
            return;
        }

        if (message.Channel.GetChannelType() != ChannelType.DM)
        {
            return;
        }

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(_client, message);

        if (!(message.HasCharPrefix('!', ref argPos) ||
              message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
        {
            var command = _discordCommandService.Search("help").Commands[0];
            command.ExecuteAsync(
                context: context,
                argList: new object[] { },
                paramList: new object[] { },
                services: null);
            return;
        }

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        await _discordCommandService.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: null);
    }

    public void SendMessage(ulong userId, string message)
    {
        try
        {
            var user = _client.GetUser(userId);
            user.SendMessageAsync(message).Wait();
            Logger.LogEverywhere($"Sent message '{message}' to user: {user.Username}:{user.Id}");
        }
        catch (Exception e)
        {
            Logger.LogEverywhere($"Received exception {e}");
        }
    }

    public void SendMessage(ISocketMessageChannel channel, string message,
        ulong? messageReferenceId = null)
    {
        try
        {
            if (messageReferenceId.HasValue)
                channel.SendMessageAsync(message, messageReference: new MessageReference(messageReferenceId)).Wait();
            else
                channel.SendMessageAsync(message).Wait();
            Logger.LogEverywhere($"Sent message '{message}' to chatId: {channel.Name}:{channel.Id}");
        }
        catch (Exception e)
        {
            Logger.LogEverywhere($"Received exception {e}");
        }
    }
}