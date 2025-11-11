using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public class ForgeListOfLinks
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }
}
