namespace FluxGuardian.FluxApi.SDK;

public class FluxPortSet
{
    public int UIPort { get; }
    public int ApiPort { get; }
    public int SyncthingPort { get; }

    public int Key
    {
        get => UIPort;
    }

    public FluxPortSet(int uiPort, int apiPort, int syncthingPort)
    {
        UIPort = uiPort;
        ApiPort = apiPort;
        SyncthingPort = syncthingPort;
    }

    public int[] GetAllPorts()
    {
        return new[] { UIPort, ApiPort, SyncthingPort };
    }

    public override string ToString()
    {
        return $"{UIPort}-{SyncthingPort}";
    }
}