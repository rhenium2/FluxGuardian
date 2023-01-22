using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services;
using FluxGuardian.Services.Discord;
using FluxGuardian.Services.Telegram;
using Newtonsoft.Json;

namespace FluxGuardian;

public class Program
{
    public static FluxConfig? FluxConfig;
    
    public static async Task Main(string[] args)
    {
        FluxConfig = JsonConvert.DeserializeObject<FluxConfig>(File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
        Logger.Log("FluxGuardian started. Press CTRL+C to exit...");
        var allUsers = Database.DefaultInstance.Users.FindAll().ToList();
        foreach (var user in allUsers)
        {
            Logger.Log($"User {user.TelegramUsername}:{user.TelegramChatId}");
            foreach (var userNode in user.Nodes)
            {
                Logger.Log($"Node {userNode.IP}:{userNode.Port}");
            }
            Logger.Log("---");
        }

        if (!string.IsNullOrWhiteSpace(FluxConfig.DiscordBotToken))
        {
            var discordService = new DiscordService(FluxConfig.DiscordBotToken);
            discordService.Start();
        }

        if (!string.IsNullOrWhiteSpace(FluxConfig.TelegramBotToken))
        {
            var telegramClient = new TelegramClient(FluxConfig.TelegramBotToken);
            var telegramCommandHandler = new TelegramCommandHandler(telegramClient);
            telegramCommandHandler.Init();
        }

        do
        {
            await NodeGuard.GetRanks();
            
            var users = Database.DefaultInstance.Users.FindAll().ToList();
            foreach (var user in users)
            {
                await NodeGuard.CheckUserNodes(user);
            }
            
            Logger.Log($"next check is in {FluxConfig.CheckFrequencyMinutes} minutes");
            Thread.Sleep(TimeSpan.FromMinutes(FluxConfig.CheckFrequencyMinutes));  
        } while (true);
    }
}