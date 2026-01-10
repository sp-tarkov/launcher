using SPTarkov.Core.Configuration;

namespace SPTarkov.Core.Mods;

public class InstallTask : IModTask
{
    public required ConfigMod Mod { get; init; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public float TotalToDownload { get; set; }
    public float Progress { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
