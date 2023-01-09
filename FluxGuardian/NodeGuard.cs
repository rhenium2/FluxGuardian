using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using Newtonsoft.Json;

namespace FluxGuardian;

public static class NodeGuard
{
    public static async Task CheckNode(string nodeUrl, TelegramClient telegramClient)
    {
        var client = new FluxApiClient(nodeUrl);
        Response response;
        try
        {
            response = await client.Get("/daemon/getzelnodestatus");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log("Node is down");
            await telegramClient.SendMessage("Node is down");
            throw;
        }
        
        var nodeStatus = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        
        if (response.Status != "success")
        {
            Logger.Log("Node is down");
            await telegramClient.SendMessage("Node is down");
            throw new Exception("Node is down");
        }

        if (nodeStatus.Status != "CONFIRMED")
        {
            Logger.Log("node is not confirmed.");
            await telegramClient.SendMessage("node is not confirmed.");
        }

        Logger.Log($"node status is {nodeStatus.Status}.");
    }
}