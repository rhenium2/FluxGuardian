namespace FluxGuardian.Models;

public class Node
{
    public string Id { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public int Rank { get; set; }
    public DateTime? LastCheckDateTime { get; set; }
    public string? LastStatus { get; set; }

    public string GetIPText()
    {
        if (Port == 16127)
        {
            return $"{IP}";
        }
        return $"{IP}:{Port}";
    }
    
    public override string ToString()
    {
        return $"http://{IP}:{Port}";
    }
}