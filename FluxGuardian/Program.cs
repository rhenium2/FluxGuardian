// See https://aka.ms/new-console-template for more information

using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services;
using Newtonsoft.Json;

namespace FluxGuardian;

public class Program
{
    public static FluxConfig? FluxConfig;
    
    public static async Task Main(string[] args)
    {
        FluxConfig = JsonConvert.DeserializeObject<FluxConfig>(File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
        Logger.Log("FluxGuardian started. Press CTRL+C to exit...");
        var allUsers = Database.Users.FindAll().ToList();
        foreach (var user in allUsers)
        {
            Logger.Log($"User {user.TelegramUsername}:{user.TelegramChatId}");
            foreach (var userNode in user.Nodes)
            {
                Logger.Log($"Node {userNode.IP}:{userNode.Port}");
            }
            Logger.Log("---");
        }

        var telegramClient = new TelegramClient(FluxConfig.TelegramBotToken);
        telegramClient.StartReceiving();

        do
        {
            await NodeGuard.GetRanks();
            
            var users = Database.Users.FindAll().ToList();
            foreach (var user in users)
            {
                await NodeGuard.CheckUserNodes(user, telegramClient);
            }
    
            Logger.Log($"next check is in {FluxConfig.CheckFrequencyMinutes} minutes");
            Thread.Sleep(TimeSpan.FromMinutes(FluxConfig.CheckFrequencyMinutes));  
        } while (true);
    }
}