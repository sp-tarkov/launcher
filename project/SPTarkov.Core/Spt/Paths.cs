namespace SPTarkov.Core.SPT;

public class Paths
{
    public static readonly string ModCache = Path.Join(Directory.GetCurrentDirectory(), "user", "Launcher", "ModCache");
    public static readonly string ModTemp = Path.Join(Directory.GetCurrentDirectory(), "user", "Launcher", "ModTemp");
    // must contain \\ for windows reg key when looking on linux/wine
    public static readonly string UninstallEftRegKey = @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov";
    public static readonly string SteamRegistryInstallKey = @"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 3932890";
    public static readonly string ProtonPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "compatibilitytools.d");
    public static readonly string SevenZip = Path.Join(Directory.GetCurrentDirectory(), "SPT_Data", "Launcher", "Dependency");
    public static readonly string PatchPath = Path.Join("SPT", "SPT_Data", "Launcher", "Patches");
    public static readonly string CoreDllPath = Path.Join("BepInEx", "plugins", "spt", "spt-core.dll");
    public static readonly string HwechoDllPath = Path.Join("EscapeFromTarkov_Data", "Plugins", "x86_64", "hwecho.dll");
    public static readonly string SptRegJson = Path.Join("user", "sptRegistry", "registry.json");
    public static readonly string LocalesPath = Path.Join(Directory.GetCurrentDirectory(), "SPT_Data", "Launcher", "Locales");
    public static readonly string LauncherAssetsPath = Path.Join(Environment.CurrentDirectory, "user", "Launcher");
    public static readonly string LauncherSettingsPath = Path.Join(Environment.CurrentDirectory, "user", "Launcher", "LauncherSettings.json");
    public static readonly string EFTSettingsFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battlestate Games", "Escape From Tarkov", "Settings"); // might have to change if BSG support linux?
    public static readonly string SPTSettingsFolder = Path.Join("SPT", "user", "sptsettings");
    public static readonly string SPTAppDataFolder = Path.Join("SPT", "user", "sptappdata");
    public static readonly List<string> ArchiveFileInfoToIgnore =
    [
        "bepinex",
        Path.Join("bepinex", "plugins"),
        "spt",
        Path.Join("spt", "user"),
        Path.Join("spt", "user", "mods")
    ];
}
