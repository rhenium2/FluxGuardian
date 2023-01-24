using System.Collections.Concurrent;
using System.Net.Sockets;
using FluxGuardian.FluxApi.SDK;
using FluxGuardian.Helpers;
using FluxGuardian.Models;
using Newtonsoft.Json;

namespace FluxGuardian.Services;

public static class NodeService
{
    public static Node CreateNode(string ip, int port)
    {
        var newNode = new Node
        {
            Id = Guid.NewGuid().ToString(),
            IP = ip,
            Port = port
        };
        return newNode;
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
        Logger.Log($"checking portset {portSet} for {ip}");

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

    // TODO: finish this refactoring
    private static string GetNodeStatus(Node node)
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
            Logger.Log(message);
            return "Not Reachable";
        }

        if (!response.Status.ToLowerInvariant().Equals("success"))
        {
            var message = $"node {node} API response is {response.Status}";
            Logger.Log(message);
            return response.Status;
        }

        var nodeStatusResponse = JsonConvert.DeserializeObject<NodeStatusResponse>(response.Data);
        var nodeStatus = nodeStatusResponse.Status;
        if (!nodeStatus.ToLowerInvariant().Equals("confirmed"))
        {
            var message = $"node {node} is not confirmed";
            Logger.Log(message);
        }

        Logger.Log($"node {node} status is {nodeStatus}.");
        return nodeStatus;
    }
}