using SPTarkov.Core.Forge;

namespace SPTarkov.Core.Mods;

public class DownloadTask : IModTask
{
    public required ForgeBase ForgeMod { get; init; }
    public required ForgeModVersion Version { get; init; }
    public float TotalToDownload { get; set; }
    public float Progress { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
