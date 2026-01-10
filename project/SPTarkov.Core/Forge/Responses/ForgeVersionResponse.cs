using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeVersionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("data")]
    public List<ForgeModVersion>? Data { get; init; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; init; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; init; }
}
