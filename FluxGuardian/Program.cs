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

    public static async Task Main(string[] args)
    {
        FluxConfig =
            JsonConvert.DeserializeObject<FluxConfig>(
                File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
        Logger.Log(
            $"FluxGuardian v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} started. Press CTRL+C to exit...");

        if (FluxConfig.DiscordBotConfig is not null)
        {
            var discordService = new DiscordService(FluxConfig.DiscordBotConfig.Token);
            discordService.Start();
        }

        if (FluxConfig.TelegramBotConfig is not null)
        {
            var telegramClient = new TelegramClient(FluxConfig.TelegramBotConfig.Token);
            var telegramCommandHandler = new TelegramCommandHandler(telegramClient);
            telegramCommandHandler.Init();
        }

        do
        {
            await NodeGuard.FetchAllNodeStatuses();

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