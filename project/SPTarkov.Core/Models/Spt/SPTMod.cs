using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models.Spt;

public record SptMod
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
