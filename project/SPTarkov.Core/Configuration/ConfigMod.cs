using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Configuration;

public class ConfigMod
{
    public string ModName { get; set; } = "unknown";

    [JsonConverter(typeof(SemVerConverter))]
    public Version ModVersion { get; set; } = new Version(0, 0, 0);
    public string GUID { get; set; } = "com.unknown.mod";
    public bool IsInstalled { get; set; } = false;
    public bool IsInstalling { get; set; } = false;
    public bool CanBeUpdated { get; set; } = false;
    public List<string>? Files { get; set; } = new List<string>();
    [JsonConverter(typeof(SemVerDictConverter))]
    public Dictionary<string, Version>? Dependencies { get; set; } = new Dictionary<string, Version>();
}
