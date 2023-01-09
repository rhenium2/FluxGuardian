// See https://aka.ms/new-console-template for more information

using FluxGuardian;
using FluxGuardian.Helpers;
using Newtonsoft.Json;

var fluxConfig = JsonConvert.DeserializeObject<FluxConfig>(File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
Logger.Log("FluxGuardian started. Press CTRL+C to exit...");
Logger.Log($"Watching {string.Join(", ", fluxConfig.NodesToWatch.Select(x=>x.ToString()))}");

var telegramClient = new TelegramClient(fluxConfig.TelegramBotToken, fluxConfig.TelegramChatId);
telegramClient.StartReceiving();

do
{
    foreach (var nodeInfo in fluxConfig.NodesToWatch)
    {
        Logger.Log($"checking {nodeInfo}...");
        var status = await NodeGuard.CheckNode(nodeInfo, telegramClient);
        NodeGuard.LastStatus[nodeInfo] = status;
    }
    
    Logger.Log($"next check is in {fluxConfig.CheckFrequencyMinutes} minutes");
    Thread.Sleep(TimeSpan.FromMinutes(fluxConfig.CheckFrequencyMinutes));  
} while (true);