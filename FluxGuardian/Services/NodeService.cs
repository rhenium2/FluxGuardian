using System.Collections.Concurrent;
using System.Net.Sockets;
using FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public enum NodeStatus
{
    Unknown,
    NotReachable,
    Error,
    Confirmed
}

public static class NodeService
{
    private static Dictionary<string, NodeStatusResponse> NodeStatusResponses = new();

    public static List<FluxPortSet> GetUserNodePortSets(User user)
    {
        var result = new List<FluxPortSet>();
        foreach (var node in user.Nodes)
        {
            result.Add(FindPortSet(node.Port));
        }

        return result;
    }

    public static List<FluxPortSet> FindActivePortSets(string ip)
    {
        var result = new ConcurrentBag<FluxPortSet>();
        Parallel.ForEach(Constants.FluxPortSets, pair =>
        {
            var portSet = pair.Value;
            if (CheckPort(ip, portSet.UIPort))
            {
                result.Add(portSet);
            }
        });

        return result.ToList();
    }

    public static FluxPortSet FindPortSet(int anyPort)
    {
        return Constants.FluxPortSets.Values.FirstOrDefault(x => x.GetAllPorts().Contains(anyPort));
    }

    public static FluxPortSet FindPortSetByIp(string ip)
    {
        var ipItems = ip.Split(':');
        if (ipItems.Length == 1)
        {
            return Constants.DefaultPortSet;
        }

        return FindPortSet(Convert.ToInt32(ipItems[1]));
    }

    public static (bool, List<int>) CheckPortSet(Node node, FluxPortSet portSet)
    {
        return CheckPortSet(node.IP, portSet);
    }

    public static (bool, List<int>) CheckPortSet(string ip, FluxPortSet portSet)
    {
        var unreachablePorts = new ConcurrentBag<int>();
        Logger.LogOutput($"checking portset {portSet} for {ip}");

        Parallel.ForEach(portSet.GetAllPorts(), port =>
        {
            if (!CheckPort(ip, port))
            {
                unreachablePorts.Add(port);
            }
        });

        return (!unreachablePorts.Any(), unreachablePorts.ToList());
    }

    private static bool CheckPort(string ip, int port)
    {
        using var tcpClient = new TcpClient();
        bool canConnect;
        try
        {
            canConnect = tcpClient.ConnectAsync(ip, port).Wait(3000);
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            tcpClient.Close();
        }

        return canConnect;
    }

    public static NodeStatusResponse? GetNodeListing(string ipPort)
    {
        if (NodeStatusResponses.Any() && NodeStatusResponses.ContainsKey(ipPort))
        {
            return NodeStatusResponses[ipPort];
        }

        return null;
    }

    public static async Task FetchAllNodeStatuses()
    {
        Logger.LogOutput($"getting all node statuses ...");

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

        Logger.LogOutput($"done.");
    }

    public static NodeStatus GetNodeStatus(Node node)
    {
        var nodePortSet = Constants.FluxPortSets[node.Port];
        using var client = new FluxApiClient($"http://{node.IP}:{nodePortSet.ApiPort}");
        Response response;
        try
        {
            response = client.Get("/daemon/getzelnodestatus").Result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var message = $"node {node} is not reachable";
            Logger.LogOutput(message);
            return NodeStatus.NotReachable;
        }

        if (!response.Status.ToLowerInvariant().Equals("success"))
        {
            var message = $"node {node} API response is {response.Status}";
            Logger.LogOutput(message);
            return NodeStatus.Error;
        }

        var nodeStatusResponse = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        var nodeStatus = nodeStatusResponse.Status;
        Logger.LogOutput($"node {node} status is {nodeStatus}.");

        if (nodeStatus.ToLowerInvariant().Equals("confirmed"))
        {
            return NodeStatus.Confirmed;
        }

        return NodeStatus.Error;
    }
}