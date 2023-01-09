using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public static class NodeGuard
{
    public static Dictionary<NodeInfo, NodeCheckInfo> LastStatus = new ();

    public static async Task CheckNodes(NodeInfo[] nodes, TelegramClient telegramClient)
    {
        foreach (var nodeInfo in nodes)
        {
            Logger.Log($"checking {nodeInfo}...");
            var status = await CheckNode(nodeInfo, telegramClient);
            LastStatus[nodeInfo] = new NodeCheckInfo { Status = status, DateTime = DateTime.UtcNow };
        }
    }
    
    private static async Task<string> CheckNode(NodeInfo nodeInfo, TelegramClient telegramClient)
    {
        var client = new FluxApiClient(nodeInfo.ToString());
        Response response;
        try
        {
            response = await client.Get("/daemon/getzelnodestatus");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var message = $"node {nodeInfo} is not reachable";
            Logger.Log(message);
            await telegramClient.SendMessage(message);
            throw;
        }
        
        var nodeStatus = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        
        if (response.Status != "success")
        {
            var message = $"node {nodeInfo} is down";
            Logger.Log(message);
            await telegramClient.SendMessage(message);
            throw new Exception(message);
        }

        if (nodeStatus.Status != "CONFIRMED")
        {
            var message = $"node {nodeInfo} is not confirmed";
            Logger.Log(message);
            await telegramClient.SendMessage(message);
        }

        Logger.Log($"node {nodeInfo} status is {nodeStatus.Status}.");
        return nodeStatus.Status;
    }
}