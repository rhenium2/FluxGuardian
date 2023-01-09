// See https://aka.ms/new-console-template for more information

using FluxGuardian;
using FluxGuardian.Helpers;
using Newtonsoft.Json;

Console.WriteLine("Hello, World!");
var fluxConfig = JsonConvert.DeserializeObject<FluxConfig>(File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
Logger.Log("FluxGuardian started. Press CTRL+C to exit...");
Logger.Log($"node={fluxConfig.Node}");

var telegramClient = new TelegramClient(fluxConfig.TelegramBotToken, fluxConfig.TelegramChatId);
do
{
    Logger.Log("Checking...");
    await NodeGuard.CheckNode(fluxConfig.Node, telegramClient);
    
    Logger.Log($"next check is in {fluxConfig.NodeCheckMinutes}mins");
    Thread.Sleep(TimeSpan.FromMinutes(fluxConfig.NodeCheckMinutes));    
} while (true);