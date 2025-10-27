using System.Text.Json.Serialization;

namespace SPTarkov.Core.Spt;

public record SptData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
