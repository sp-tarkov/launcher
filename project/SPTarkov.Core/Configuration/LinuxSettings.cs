namespace SPTarkov.Core.Configuration;

public record LinuxSettings
{
    // /home/cwx/Games/tarkov
    public string? PrefixPath { get; set; } = Directory.GetCurrentDirectory().Split("/drive_c").FirstOrDefault();

    // /home/cwx/.local/share/spt-additions/runtime/umu-run
    public string UmuPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local/share/spt-additions/runtime/umu-run");

    // ENVVAR1=1 ENVVAR2=2,yes=1 -Arg1="arg1 space" -Arg2=arg2 - similar system to steam
    public string LaunchSettings { get; set; } = "";

    // "GE-Proton10-24" "GE-Proton10-20"
    public string ProtonVersion { get; set; } = "GE-Proton10-24";
}
