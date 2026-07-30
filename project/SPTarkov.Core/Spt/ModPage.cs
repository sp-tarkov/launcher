using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record ModPage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("homePage")]
    public string HomePage { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
