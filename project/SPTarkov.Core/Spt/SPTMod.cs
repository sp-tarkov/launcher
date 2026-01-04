using System.ComponentModel;
using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.SPT;

public record SptMod
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    [JsonConverter(typeof(SemVerVersionConverter))]
    public Version Version { get; set; } = new Version(0, 0, 0);

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
