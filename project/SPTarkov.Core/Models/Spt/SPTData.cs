using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record SPTData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
