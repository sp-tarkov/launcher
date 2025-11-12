namespace SPTarkov.Core.Configuration;

public class ConfigMod
{
    public string ModName { get; set; } = "";
    public string GUID { get; set; } = "";
    public bool IsInstalled { get; set; } = false;
    public bool CanBeUpdated { get; set; } = false;
    public List<string>? Files { get; set; } = new List<string>();
}
