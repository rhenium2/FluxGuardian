using Newtonsoft.Json;

namespace FluxGuardian.FluxApi.SDK;

public class NodeStatusResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("collateral")]
    public string Collateral { get; set; }

    [JsonProperty("txhash")]
    public string Txhash { get; set; }

    [JsonProperty("outidx")]
    public string Outidx { get; set; }

    [JsonProperty("ip")]
    public string Ip { get; set; }

    [JsonProperty("network")]
    public string Network { get; set; }

    [JsonProperty("added_height")]
    public int AddedHeight { get; set; }

    [JsonProperty("confirmed_height")]
    public int ConfirmedHeight { get; set; }

    [JsonProperty("last_confirmed_height")]
    public int LastConfirmedHeight { get; set; }

    [JsonProperty("last_paid_height")]
    public int LastPaidHeight { get; set; }

    [JsonProperty("tier")]
    public string Tier { get; set; }

    [JsonProperty("payment_address")]
    public string PaymentAddress { get; set; }

    [JsonProperty("pubkey")]
    public string Pubkey { get; set; }

    [JsonProperty("activesince")]
    public string Activesince { get; set; }

    [JsonProperty("lastpaid")]
    public string Lastpaid { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("rank")]
    public int Rank { get; set; }
}