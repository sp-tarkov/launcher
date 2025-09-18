using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeModsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ForgeBase>? Data { get; set; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; set; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; set; }
}
