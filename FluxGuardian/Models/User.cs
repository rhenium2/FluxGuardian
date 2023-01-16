namespace FluxGuardian.Models;

public class User
{
    public int Id { get; set; }
    public string TelegramUsername { get; set; }
    public long TelegramChatId { get; set; }
    public bool TelegramBlocked { get; set; }
    public string DiscordUsername { get; set; }
    public long DiscordId { get; set; }
    public List<Node> Nodes { get; set; }
    public string? ActiveCommand { get; set; }
    public Dictionary<string, string?> ActiveCommandParams { get; set; }

    public User()
    {
        Nodes = new List<Node>();
        ActiveCommandParams = new Dictionary<string, string?>();
    }
}