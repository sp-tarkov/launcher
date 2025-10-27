using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public record ForgeResponseData
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
