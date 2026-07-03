using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public class ForgeListOfLinks
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("label")]
    public required string Label { get; set; }
}
