using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record ForgeLoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public ForgeResponseData? Data { get; set; }
}
