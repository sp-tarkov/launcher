using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeModResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ForgeBase? Data { get; set; }
}
