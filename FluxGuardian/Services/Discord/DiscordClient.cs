using Discord;
using Discord.WebSocket;
using FluxGuardian.Helpers;

namespace FluxGuardian.Services.Discord;

public static class DiscordClient
{
    public static void SendMessage(ISocketMessageChannel channel, string message,
        ulong? messageReferenceId = null)
    {
        try
        {
            if(messageReferenceId.HasValue)
                channel.SendMessageAsync(message, messageReference: new MessageReference(messageReferenceId)).Wait();
            else 
                channel.SendMessageAsync(message).Wait();
            Logger.Log($"Sent message '{message}' to chatId: {channel.Name}:{channel.Id}");
            Logger.LogMessage($"Sent message '{message}' to chatId: {channel.Name}:{channel.Id}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log($"Received exception {e}");
            Logger.LogMessage($"Received exception {e}");
        }
    }
}