namespace FluxGuardian.Models;

public class Node
{
    public string Id { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public DateTime? LastCheckDateTime { get; set; }
    public string? LastStatus { get; set; }
    
    public override string ToString()
    {
        return $"http://{IP}:{Port}";
    }
}