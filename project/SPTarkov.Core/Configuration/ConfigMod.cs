namespace SPTarkov.Core.Configuration;

public class ConfigMod
{
    public string ModName { get; set; } = "unknown";
    public string ModVersion { get; set; } = "0.0.0";
    public string GUID { get; set; } = "com.unknown.mod";
    public bool IsInstalled { get; set; } = false;
    public bool CanBeUpdated { get; set; } = false;
    public List<string>? Files { get; set; } = new List<string>();
}
