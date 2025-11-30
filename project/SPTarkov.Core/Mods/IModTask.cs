namespace SPTarkov.Core.Mods;

public interface IModTask
{
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public float TotalToDownload { get; set; }
    public float Progress { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
