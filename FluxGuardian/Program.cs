using System.Reflection;
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
    public static DiscordClient DiscordClient;
    public static TelegramClient TelegramClient;

    public static async Task Main(string[] args)
    {
        FluxConfig =
            JsonConvert.DeserializeObject<FluxConfig>(
                File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
        Logger.LogOutput(
            $"FluxGuardian v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} started. Press CTRL+C to exit...");

        if (FluxConfig.DiscordBotConfig is not null)
        {
            DiscordClient = new DiscordClient(FluxConfig.DiscordBotConfig.Token);
            DiscordClient.Init();
        }

        if (FluxConfig.TelegramBotConfig is not null)
        {
            TelegramClient = new TelegramClient(FluxConfig.TelegramBotConfig.Token);
            TelegramClient.Init();
        }

        do
        {
            await NodeService.FetchAllNodeStatuses();

            var users = Database.DefaultInstance.Users.FindAll().ToList();
            foreach (var user in users)
            {
                await NodeChecker.CheckUserNodes(user);
            }

            Logger.LogOutput($"next check is in {FluxConfig.CheckFrequencyMinutes} minutes");
            Thread.Sleep(TimeSpan.FromMinutes(FluxConfig.CheckFrequencyMinutes));
        } while (true);
    }
}