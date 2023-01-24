using Newtonsoft.Json;

namespace FluxGuardian.Models;

public class FluxConfig
{
    [JsonProperty("telegramBotConfig", Required = Required.Always)]
    public TelegramBotConfig TelegramBotConfig { get; set; }
    
    [JsonProperty("discordBotConfig")]
    public DiscordBotConfig DiscordBotConfig { get; set; }
    
    [JsonProperty("checkFrequencyMinutes", Required = Required.Always)]
    public int CheckFrequencyMinutes { get; set; }
}

public class TelegramBotConfig
{
    [JsonProperty("token", Required = Required.Always)]
    public string Token { get; set; }
    [JsonProperty("moderatorChatId")]
    public long ModeratorChatId { get; set; }
}

public class DiscordBotConfig
{
    [JsonProperty("token", Required = Required.Always)]
    public string Token { get; set; }
}