using SPTarkov.Core.Forge;

namespace SPTarkov.Core.Mods;

public class DownloadTask : IModTask
{
    public required ForgeBase ForgeMod { get; set; }
    public required ForgeModVersion Version { get; set; }
    public float TotalToDownload { get; set; }
    public float Progress { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
