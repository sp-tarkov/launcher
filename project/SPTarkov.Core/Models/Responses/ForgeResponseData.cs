using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models.Responses;

public record ForgeResponseData
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
