using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeModResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public ForgeBase? Data { get; init; }
}
