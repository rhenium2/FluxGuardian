using FluxGuardian.Data;
using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public static class NodeGuard
{
    //public static Dictionary<NodeInfo, NodeCheckInfo> LastStatus = new ();

    public static async Task CheckNodes(User user, TelegramClient telegramClient)
    {
        foreach (var node in user.Nodes)
        {
            Logger.Log($"checking {node}...");
            var status = await CheckNode(user, node, telegramClient);
            node.LastCheckDateTime = DateTime.UtcNow;
            node.LastStatus = status;
            Database.Users.Update(user);
        }
    }
    
    private static async Task<string> CheckNode(User user, Node node, TelegramClient telegramClient)
    {
        var client = new FluxApiClient(node.ToString());
        Response response;
        try
        {
            response = await client.Get("/daemon/getzelnodestatus");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var message = $"node {node} is not reachable";
            Logger.Log(message);
            await telegramClient.SendMessage(user.TelegramChatId, message);
            throw;
        }
        
        var nodeStatus = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        
        if (response.Status != "success")
        {
            var message = $"node {node} response is {response.Status}";
            Logger.Log(message);
            await telegramClient.SendMessage(user.TelegramChatId, message);
            throw new Exception(message);
        }

        if (nodeStatus.Status != "CONFIRMED")
        {
            var message = $"node {node} is not confirmed";
            Logger.Log(message);
            await telegramClient.SendMessage(user.TelegramChatId, message);
        }

        Logger.Log($"node {node} status is {nodeStatus.Status}.");
        return nodeStatus.Status;
    }
}