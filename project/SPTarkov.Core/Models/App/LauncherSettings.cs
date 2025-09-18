namespace SPTarkov.Core.Models;

public class LauncherSettings
{
    public LauncherSettings()
    {
        Servers.Add(new Server
        {
            IpAddress = "127.0.0.1:6969",
            Name = "LocalHost",
            ServerId = "1721162719",
            Locked = true
        });
        GamePath = Environment.CurrentDirectory;
    }

    public DebugSettings DebugSettings { get; set; } = new();
    public List<Server> Servers { get; set; } = new();
    public StartLocation StartLocation { get; set; } = new();
    public StartSize StartSize { get; set; } = new();
    public bool FirstRun { get; set; } = true;
    public string GamePath { get; set; } = "";
    public bool CloseToTray { get; set; } = false;
    public bool MinimizeOnLaunch { get; set; } = true;
    public bool AlwaysTop { get; set; } = false;
    public bool AdvancedUser { get; set; } = false;
    public string ForgeApiKey { get; set; } = "";
    public bool UseBackground { get; set; } = false;
}
