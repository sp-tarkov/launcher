namespace SPTarkov.Core.Mods;

public interface IModTask
{
    public CancellationTokenSource CancellationTokenSource { get; }
    public float Progress { get; }
    public bool Complete { get; }
    public Exception? Error { get; }
}
