using Newtonsoft.Json;

namespace FluxGuardian;

public class FluxConfig
{
    [JsonProperty("nodesToWatch", Required = Required.Always)]
    public List<NodeInfo> NodesToWatch { get; set; }
    
    [JsonProperty("telegramBotToken", Required = Required.Always)]
    public string TelegramBotToken { get; set; }
    
    [JsonProperty("telegramChatId", Required = Required.Always)]
    public long TelegramChatId { get; set; }
    
    [JsonProperty("checkFrequencyMinutes", Required = Required.Always)]
    public int CheckFrequencyMinutes { get; set; }
}

public class NodeInfo
{
    [JsonProperty("ip", Required = Required.Always)]
    public string IP { get; set; }
    [JsonProperty("port", Required = Required.Always)]
    public int Port { get; set; }

    public override string ToString()
    {
        return $"{IP}:{Port}";
    }
}