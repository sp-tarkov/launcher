namespace SPTarkov.Core.Mods;

public class UpdateTask
{
    public string ModName { get; set; }
    public string Version { get; set; }
    public string GUID { get; set; }
    public string Link { get; set; }
    public float Progress { get; set; }
    public float TotalToDownload { get; set; }
    public required CancellationTokenSource CancellationToken { get; set; }
    public bool Complete { get; set; }
    public Exception Error { get; set; }
}
