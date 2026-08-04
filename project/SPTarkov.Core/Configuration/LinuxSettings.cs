namespace SPTarkov.Core.Configuration;

public record LinuxSettings
{
    /// <summary>Wine prefix root, e.g. <c>/home/cwx/Games/tarkov</c>.</summary>
    public string PrefixPath { get; set; } = Directory.GetCurrentDirectory().Split("/drive_c")[0];

    /// <summary>Path to the <c>umu-run</c> binary, e.g. <c>/home/cwx/.local/share/spt-additions/runtime/umu-run</c>.</summary>
    public string UmuPath { get; set; } =
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/spt-additions/runtime/umu-run");

    /// <summary>
    /// Extra env vars and arguments in Steam launch-options format, e.g. <c>ENVVAR1=1 -Arg1="arg1 space" -Arg2=arg2</c>.
    /// </summary>
    public string LaunchSettings { get; set; } = "";

    /// <summary>Proton build name, e.g. <c>GE-Proton10-24</c>.</summary>
    public string ProtonVersion { get; set; } = "GE-Proton11-1";

    public bool GameMode { get; set; }

    /// <summary>Default environment variables required to run the client.</summary>
    public string DefaultEnv { get; set; } = @"WINEDLLOVERRIDES=""winhttp=n,b""";
}
