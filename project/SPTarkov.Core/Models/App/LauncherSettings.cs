namespace SPTarkov.Core.Models.App;

public record LauncherSettings
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
    }

    public List<Server> Servers { get; set; } = new();

    public StartLocation StartLocation { get; set; } = new();

    public StartSize StartSize { get; set; } = new();

    public bool FirstRun { get; set; } = true;

    public string GamePath { get; set; } = "";

    public bool CloseToTray { get; set; }

    public bool MinimizeOnLaunch { get; set; } = true;

    public bool AlwaysTop { get; set; }

    public bool AdvancedUser { get; set; }

    public string ForgeApiKey { get; set; } = "";

    public bool UseBackground { get; set; }

    public List<string> ExcludeFromCleanup { get; set; } = new();

    public string Language { get; set; } = "en";
}
