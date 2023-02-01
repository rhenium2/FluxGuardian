namespace FluxGuardian.Models;

public class User
{
    public int Id { get; set; }
    public string TelegramUsername { get; set; }
    public long TelegramChatId { get; set; }
    public string DiscordUsername { get; set; }
    public ulong DiscordId { get; set; }
    public List<Node> Nodes { get; set; }
    public string? ActiveCommand { get; set; }
    public Dictionary<string, string?> ActiveCommandParams { get; set; }

    public User()
    {
        Nodes = new List<Node>();
        ActiveCommandParams = new Dictionary<string, string?>();
    }

    public ContextKind GetUserContextKind()
    {
        if (TelegramChatId != 0)
        {
            return ContextKind.Telegram;
        }

        if (DiscordId != 0)
        {
            return ContextKind.Discord;
        }

        throw new Exception("UserContextKind is not available");
    }

    public override string ToString()
    {
        if (TelegramChatId != 0)
        {
            var usernameText = !string.IsNullOrWhiteSpace(TelegramUsername) ? $"({TelegramUsername})" : "";
            return $"{TelegramChatId}{usernameText}";
        }

        if (DiscordId != 0)
        {
            var usernameText = !string.IsNullOrWhiteSpace(DiscordUsername) ? $"({DiscordUsername})" : "";
            return $"{DiscordId}{usernameText}";
        }

        return base.ToString();
    }
}