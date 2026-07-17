using SPTarkov.Core.Configuration;

namespace SPTarkov.Core.Mods;

public class InstallTask : IModTask
{
    public required ConfigMod ForgeMod { get; init; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public long TotalToDownload { get; set; }
    public float Progress { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
