using FluxGuardian.Data;
using FluxGuardian.Helpers;
using FluxGuardian.Models;

namespace FluxGuardian.Services;

public static class NodeChecker
{
    public static async Task CheckUserNodes(User user)
    {
        foreach (var node in user.Nodes)
        {
            Logger.LogOutput($"checking {node}...");
            Notifier.NotifyUser(user, $"checking {node}...");
            /// checking ports
            var nodePortSet = NodeService.FindPortSet(node.Port);
            var (portStatus, closedPorts) = NodeService.CheckPortSet(node, nodePortSet);
            node.ClosedPorts = closedPorts;

            /// checking api status
            var nodeStatus = NodeService.GetNodeStatus(node);
            node.LastCheckDateTime = DateTime.UtcNow;
            node.LastStatus = nodeStatus;

            /// checking rank
            var key = node.ToIPAndPortText();
            var nodeListing = NodeService.GetNodeListing(key);
            if (nodeListing != null)
            {
                node.Rank = nodeListing.Rank;
                node.Tier = nodeListing.Tier;
            }

            /// Save in DB
            Database.DefaultInstance.Users.Update(user);

            NotifyUser(user, node);
        }

        Database.DefaultInstance.Users.Update(user);
    }

    private static async Task NotifyUser(User user, Node node)
    {
        if (node.ClosedPorts.Any())
        {
            var message = "Problem!\n" +
                          $"Port(s) {string.Join(",", node.ClosedPorts.ToArray())} is not open for node {node}";
            Notifier.NotifyUser(user, message);
            Logger.LogOutput(message);
        }

        if (node.LastStatus == NodeStatus.NotReachable)
        {
            var message = "Problem!\n" + $"node {node} is not reachable";
            Logger.LogOutput(message);
            Notifier.NotifyUser(user, message);
        }

        if (node.LastStatus == NodeStatus.Error)
        {
            var message = "Problem!\n" + $"node {node} is not confirmed in FLUX network";
            Logger.LogOutput(message);
            Notifier.NotifyUser(user, message);
        }
    }
}