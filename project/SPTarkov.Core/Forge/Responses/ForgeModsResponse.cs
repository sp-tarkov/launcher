using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeModsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public List<ForgeBase>? Data { get; init; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; init; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; init; }
}
