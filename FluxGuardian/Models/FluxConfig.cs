using Newtonsoft.Json;

namespace FluxGuardian.Models;

public class FluxConfig
{
    
    [JsonProperty("telegramBotToken", Required = Required.Always)]
    public string TelegramBotToken { get; set; }
    [JsonProperty("checkFrequencyMinutes", Required = Required.Always)]
    public int CheckFrequencyMinutes { get; set; }
    [JsonProperty("mChatId")]
    public long MChatId { get; set; }
}