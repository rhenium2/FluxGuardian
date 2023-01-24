using System.Net.Sockets;
using FluxGuardian.Data;
using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using FluxGuardian.Services.Telegram;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public static class NodeGuard
{
    private static Dictionary<string, NodeStatusResponse> NodeStatusResponses = new();
    private static TelegramClient _telegramClient;

    static NodeGuard()
    {
        _telegramClient = new TelegramClient(Program.FluxConfig.TelegramBotConfig.Token);
    }

    public static async Task CheckUserNodes(User user)
    {
        foreach (var node in user.Nodes)
        {
            Logger.Log($"checking {node}...");
            /// checking ports
            var (portStatus, closedPorts) = await CheckNodePorts(user, node);
            node.ClosedPorts = closedPorts;

            /// checking api status
            var nodeStatus = await CheckNodeStatus(user, node);
            node.LastCheckDateTime = DateTime.UtcNow;
            node.LastStatus = nodeStatus;

            /// checking rank
            //var nodeFullIp = node.GetLookupText();
            var key = node.ToIPAndPortText();
            if (NodeStatusResponses.Any() && NodeStatusResponses.ContainsKey(key))
            {
                node.Rank = NodeStatusResponses[key].Rank;
                node.Tier = NodeStatusResponses[key].Tier;
            }
        }

        Database.DefaultInstance.Users.Update(user);
    }

    private static async Task<string> CheckNodeStatus(User user, Node node)
    {
        var nodePortSet = Constants.FluxPortSets[node.Port];
        using var client = new FluxApiClient($"http://{node.IP}:{nodePortSet.ApiPort}");
        Response response;
        try
        {
            response = await client.Get("/daemon/getzelnodestatus");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var message = "Problem!\n" + $"node {node} is not reachable";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
            return "Not Reachable";
        }

        if (!response.Status.ToLowerInvariant().Equals("success"))
        {
            var message = "Problem!\n" + $"node {node} API response is {response.Status}";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
            return response.Status;
        }

        var nodeStatusResponse = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        var nodeStatus = nodeStatusResponse.Status;
        if (!nodeStatus.ToLowerInvariant().Equals("confirmed"))
        {
            var message = "Problem!\n" + $"node {node} is not confirmed";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
        }

        Logger.Log($"node {node} status is {nodeStatus}.");
        return nodeStatus;
    }

    private static async Task<(bool, List<int>)> CheckNodePorts(User user, Node node)
    {
        var nodePortSet = Constants.FluxPortSets[node.Port];

        var (portSetReachable, unreachablePorts) = NodeService.CheckPortSet(node, nodePortSet);

        if (!portSetReachable)
        {
            var message = "Problem!\n" +
                          $"Port(s) {string.Join(",", unreachablePorts.ToArray())} is not open for node {node}";
            Logger.Log(message);
            _telegramClient.SendMessage(user.TelegramChatId, message);
        }

        return (portSetReachable, unreachablePorts);
    }

    public static async Task FetchAllNodeStatuses()
    {
        Logger.Log($"getting all node statuses ...");

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
        NodeStatusResponses.Clear();
        foreach (var nodeStatusResponse in nodeStatusResponses)
        {
            if (string.IsNullOrWhiteSpace(nodeStatusResponse.Ip))
            {
                continue;
            }

            var portSet = NodeService.FindPortSetByIp(nodeStatusResponse.Ip);
            var key = $"{nodeStatusResponse.Ip.Split(':')[0]}:{portSet.Key}";

            if (!NodeStatusResponses.ContainsKey(key))
            {
                NodeStatusResponses.Add(key, nodeStatusResponse);
            }
            else
            {
            }
        }

        Logger.Log($"done.");
    }
}