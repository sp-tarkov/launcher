using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeModResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ForgeBase? Data { get; set; }
}
