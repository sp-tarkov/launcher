using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record ForgeResponseData
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
