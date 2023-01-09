using Newtonsoft.Json;

namespace FluxGuardian;

public class FluxConfig
{
    [JsonProperty("node")]
    public string Node { get; set; }
    
    [JsonProperty("telegramBotToken")]
    public string TelegramBotToken { get; set; }
    
    [JsonProperty("telegramChatId")]
    public long TelegramChatId { get; set; }
    
    [JsonProperty("nodeCheckMinutes")]
    public int NodeCheckMinutes { get; set; }
}