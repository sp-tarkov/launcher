using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Configuration;

public class ConfigMod
{
    public string ModName { get; init; } = "unknown";
    public string GUID { get; set; } = "com.unknown.mod";
    public bool IsInstalled { get; set; }
    public bool IsInstalling { get; set; }
    public bool CanBeUpdated { get; set; }
    public List<string>? Files { get; set; } = [];

    [JsonConverter(typeof(SemVerVersionConverter))]
    public Version? ModVersion { get; set; } = new(0, 0, 0);
    [JsonConverter(typeof(SemVerVersionDictConverter))]
    public Dictionary<string, Version>? Dependencies { get; set; } = new();
}
