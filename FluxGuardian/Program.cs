// See https://aka.ms/new-console-template for more information

using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services;
using Newtonsoft.Json;

var fluxConfig = JsonConvert.DeserializeObject<FluxConfig>(File.ReadAllText(Directory.GetCurrentDirectory() + "/fluxconfig.json"));
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

var telegramClient = new TelegramClient(fluxConfig.TelegramBotToken);
telegramClient.StartReceiving();

do
{
    var users = Database.Users.FindAll().ToList();
    foreach (var user in users)
    {
        await NodeGuard.CheckNodes(user, telegramClient);
    }
    
    Logger.Log($"next check is in {fluxConfig.CheckFrequencyMinutes} minutes");
    Thread.Sleep(TimeSpan.FromMinutes(fluxConfig.CheckFrequencyMinutes));  
} while (true);