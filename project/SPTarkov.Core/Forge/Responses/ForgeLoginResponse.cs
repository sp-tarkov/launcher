using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeLoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("data")]
    public ForgeResponseData? Data { get; init; }
}
