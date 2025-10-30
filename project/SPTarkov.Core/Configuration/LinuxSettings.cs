namespace SPTarkov.Core.Configuration;

public record LinuxSettings
{
    // "PrefixPath": "/home/cwx/Games/tarkov",
    public string PrefixPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Games/tarkov");

    // "UmuPath": "/home/cwx/.local/share/spt-additions/runtime/umu-run"
    public string UmuPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local/share/spt-additions/runtime/umu-run");
}
