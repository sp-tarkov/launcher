using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record SptData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
