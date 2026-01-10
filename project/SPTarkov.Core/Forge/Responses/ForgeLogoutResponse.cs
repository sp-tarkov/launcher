using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeLogoutResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public ForgeResponseData? Data { get; init; }
}
