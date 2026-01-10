using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeAbilityReponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public List<string>? Data { get; init; }
}
