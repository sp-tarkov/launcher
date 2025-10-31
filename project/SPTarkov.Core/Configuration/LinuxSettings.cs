namespace SPTarkov.Core.Configuration;

public record LinuxSettings
{
    // /home/cwx/Games/tarkov
    public string? PrefixPath { get; set; } = Directory.GetCurrentDirectory().Split("drive_c").FirstOrDefault();

    // /home/cwx/.local/share/spt-additions/runtime/umu-run
    public string UmuPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local/share/spt-additions/runtime/umu-run");

    // ENVVAR1=1 ENVVAR2=2 %command% -Arg1=arg1 -Arg2=arg2 - similar system to steam
    public string LaunchSettings { get; set; } = "";
}
