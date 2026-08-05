namespace SPTarkov.Core.Configuration;

public record LauncherSettings
{
    /// <summary>User-added servers only. The locked local server comes from <see cref="Server.Local"/>.</summary>
    public List<Server> Servers { get; set; } = [];

    public StartLocation StartLocation { get; set; } = new();

    public StartSize StartSize { get; set; } = new();

    public bool FirstRun { get; set; } = true;

    public bool EnableGamePath { get; set; } = false;

    /// <summary>The SPT install directory: the parent of the launcher's working directory.</summary>
    public string GamePath { get; set; } = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;

    public bool CloseToTray { get; set; }

    public bool MinimizeOnLaunch { get; set; } = true;

    public bool AlwaysTop { get; set; }

    public bool UseBackground { get; set; } = true;

    public bool ClearCacheOnLaunch { get; set; }

    public bool AutoConnectLastProfile { get; set; }

    /// <summary>The last server/profile the user launched with.</summary>
    public PreferredProfile? PreferredProfile { get; set; }

    public List<string> ExcludeFromCleanup { get; set; } = new();

    public string Language { get; set; } = "en";

    public LinuxSettings LinuxSettings { get; set; } = new();

    public bool DebugLogging { get; set; }
}
