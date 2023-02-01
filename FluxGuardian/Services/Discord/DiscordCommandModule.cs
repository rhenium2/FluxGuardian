using Discord.Commands;
using FluxGuardian.Models;
using CommandContext = FluxGuardian.Models.CommandContext;

// ReSharper disable UnusedMember.Global

namespace FluxGuardian.Services.Discord;

public class DiscordCommandModule : ModuleBase<SocketCommandContext>
{
    public DiscordCommandModule()
    {
    }

    [Command("help")]
    public async Task Help()
    {
        var commandContext = GetCommandContext();
        CommandService.HandleHelpCommand(commandContext);
    }

    [Command("addnode")]
    public async Task AddNodeAsync(
        string ip)
    {
        var commandContext = GetCommandContext(new KeyValuePair<string, string>("ip", ip));
        CommandService.HandleAddNodeCommand(commandContext);
    }

    [Command("start")]
    public async Task HandleStartCommand()
    {
        var commandContext = GetCommandContext();

        CommandService.HandleStartCommand(commandContext);
    }

    [Command("status")]
    public async Task HandleStatusCommand()
    {
        var commandContext = GetCommandContext();

        CommandService.HandleStatusCommand(commandContext);
    }

    [Command("mynodes")]
    public async Task HandleMyNodesCommand()
    {
        var commandContext = GetCommandContext();

        CommandService.HandleMyNodesCommand(commandContext);
    }

    [Command("removeallnodes")]
    public async Task HandleRemoveAllNodesCommand()
    {
        var commandContext = GetCommandContext();

        CommandService.HandleRemoveAllNodesCommand(commandContext);
    }

    private CommandContext GetCommandContext(params KeyValuePair<string, string>[] arguments)
    {
        return new CommandContext
        {
            Message = Context.Message.ToString(),
            MessageId = Context.Message.Id.ToString(),
            Arguments = new Dictionary<string, string>(arguments),
            Username = Context.User.Username,
            UserId = Context.User.Id.ToString(),
            ContextKind = ContextKind.Discord,
            DiscordClient = Program.DiscordClient,
            DiscordChannel = Context.Channel,
        };
    }
}