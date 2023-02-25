using FluxGuardian.Services;

namespace FluxGuardian.Models;

public class Node
{
    public string Id { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public int Rank { get; set; }
    public DateTime? LastCheckDateTime { get; set; }
    public NodeStatus? LastStatus { get; set; }
    public List<int> ClosedPorts { get; set; }
    public string Tier { get; set; }
    public string FluxVersion { get; set; }

    public Node()
    {
        ClosedPorts = new List<int>();
    }

    // public string GetLookupText()
    // {
    //     if (Port == Constants.DefaultFluxPort)
    //     {
    //         return $"{IP}";
    //     }
    //
    //     var portSet = Constants.FluxPortSets[Port];
    //     return $"{IP}:{portSet.ApiPort}";
    // }

    public string ToIPAndPortText()
    {
        return $"{IP}:{Port}";
    }

    public override string ToString()
    {
        return $"http://{IP}:{Port}";
    }
}