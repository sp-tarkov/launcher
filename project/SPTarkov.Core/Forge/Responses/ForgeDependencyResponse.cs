using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeDependencyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public List<ForgeDependencyNode>? Data { get; init; }
}
