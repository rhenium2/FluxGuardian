using FluxGuardian.Data;
using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services.Telegram;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public static class NodeGuard
{
    private static Dictionary<string, int> NodeRanks = new();
    private static TelegramClient _telegramClient;
    static NodeGuard()
    {
        _telegramClient = new TelegramClient(Program.FluxConfig.TelegramBotToken);
    }

    public static async Task CheckUserNodes(User user)
    {
        foreach (var node in user.Nodes)
        {
            Logger.Log($"checking {node}...");
            var status = await CheckNode(user, node);
            node.LastCheckDateTime = DateTime.UtcNow;
            node.LastStatus = status;

            var nodeFullIp = node.GetIPText();
            if (NodeRanks.Any() && NodeRanks.ContainsKey(nodeFullIp))
            {
                node.Rank = NodeRanks[nodeFullIp];
            }
            Database.DefaultInstance.Users.Update(user);
        }
    }
    
    private static async Task<string> CheckNode(User user, Node node)
    {
        using var client = new FluxApiClient(node.ToString());
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
            _telegramClient.SendMessage(user.TelegramChatId, message);
            return "Not Reachable";
        }
        
        if (!response.Status.ToLowerInvariant().Equals("success"))
        {
            var message = $"node {node} response is {response.Status}";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
            return response.Status;
        }

        var nodeStatusResponse = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        var nodeStatus = nodeStatusResponse.Status;
        if (!nodeStatus.ToLowerInvariant().Equals("confirmed"))
        {
            var message = $"node {node} is not confirmed";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
        }

        Logger.Log($"node {node} status is {nodeStatus}.");
        return nodeStatus;
    }

    public static async Task GetRanks()
    {
        Logger.Log($"getting all node ranks ...");

        using var client = new FluxApiClient();
        Response response;
        try
        {
            response = await client.Get("/daemon/viewdeterministiczelnodelist");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.LogMessage(e.ToString());
            return;
        }
        
        var nodeStatusResponses = JsonConvert.DeserializeObject<NodeStatusResponse[]>(response.Data);
        NodeRanks.Clear();
        foreach (var nodeStatusResponse in nodeStatusResponses)
        {
            if (string.IsNullOrWhiteSpace(nodeStatusResponse.Ip))
            {
                continue;
            }
            
            if (!NodeRanks.ContainsKey(nodeStatusResponse.Ip))
            {
                NodeRanks.Add(nodeStatusResponse.Ip, nodeStatusResponse.Rank);
            }
            else
            {
                
            }
        }
        
        Logger.Log($"done.");
    }
}