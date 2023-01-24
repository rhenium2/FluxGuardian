using System.Text;
using Discord.Commands;
using FluxGuardian.Helpers;

// ReSharper disable UnusedMember.Global

namespace FluxGuardian.Services.Discord;

public class CommandModule : ModuleBase<SocketCommandContext>
{
    [Command("square")]
    [Summary("Squares a number.")]
    public async Task SquareAsync(
        [Summary("The number to square.")] int num)
    {
        // We can also access the channel from the Command Context.
        await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
    }

    [Command("addnode")]
    [Summary("Adds a new node")]
    public async Task AddNodeAsync(
        [Summary("IP of the Flux node")] string ip,
        [Summary("port of the Flux node(usually 16127)")]
        string port)
    {
        if (!Extensions.IsValidFluxPortString(port)
            || !Extensions.IsValidIPString(ip))
        {
            DiscordClient.SendMessage(Context.Channel, "IP & port don't seem valid to me. Please try again");
            return;
        }

        var user = UserService.FindDiscordUser(Context.User.Id) ??
                   UserService.CreateDiscordUser(Context.User.Username, Context.User.Id);

        if (user.Nodes.Count >= Constants.MaximumNodeCount)
        {
            DiscordClient.SendMessage(Context.Channel, "sorry, you have reached the maximum number of nodes");
            return;
        }

        var newNode = UserService.AddNode(user, ip, Convert.ToInt32(port));

        DiscordClient.SendMessage(Context.Channel,
            $"node {newNode} added. At any time you can ask me about your nodes status with _!status_ command");
    }


    [Command("start")]
    [Summary("starts a new session and shows the starting message")]
    public async Task HandleStartCommand()
    {
        var user = UserService.FindDiscordUser(Context.User.Id);
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

        DiscordClient.SendMessage(Context.Channel, text.ToString(), Context.Message.Id);
    }
}