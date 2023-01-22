using Discord.Commands;

namespace FluxGuardian.Services.Discord;

public class CommandModule :  ModuleBase<SocketCommandContext>
{
    [Command("square")]
    [Summary("Squares a number.")]
    public async Task SquareAsync(
        [Summary("The number to square.")] 
        int num)
    {
        // We can also access the channel from the Command Context.
        await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
    }

}