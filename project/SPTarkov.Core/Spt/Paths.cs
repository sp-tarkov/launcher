namespace SPTarkov.Core.SPT;

public class Paths
{
    public static readonly string ModCache = Path.Combine(Directory.GetCurrentDirectory(), "user", "Launcher", "ModCache");
    public static readonly string ModTemp = Path.Combine(Directory.GetCurrentDirectory(), "user", "Launcher", "ModTemp");
    // must contain \\ for windows reg key when looking on linux/wine
    public static readonly string UninstallEftRegKey = @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov";
    public static readonly string ProtonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "compatibilitytools.d");
    public static readonly string SevenZipDllPath = Path.Combine(Directory.GetCurrentDirectory(), "SPT_Data", "Launcher", "Dependency", "7z.dll");
    public static readonly string PatchPath = Path.Combine("SPT", "SPT_Data", "Launcher", "Patches");
    public static readonly string CoreDllPath = Path.Combine("BepInEx", "plugins", "spt", "spt-core.dll");
    public static readonly string HwechoDllPath = Path.Combine("EscapeFromTarkov_Data", "Plugins", "x86_64", "hwecho.dll");
    public static readonly string SptRegJson = Path.Combine("user", "sptRegistry", "registry.json");
    public static readonly string LocalesPath = Path.Combine(Directory.GetCurrentDirectory(), "SPT_Data", "Launcher", "Locales");
    public static readonly string LauncherAssetsPath = Path.Combine(Environment.CurrentDirectory, "user", "Launcher");
    public static readonly string LauncherSettingsPath = Path.Combine(Environment.CurrentDirectory, "user", "Launcher", "LauncherSettings.json");
    public static readonly string EFTSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battlestate Games", "Escape From Tarkov", "Settings"); // might have to change if BSG support linux?
    public static readonly string SPTSettingsFolder = Path.Combine("user", "sptsettings");
    public static readonly string SPTAppDataFolder = Path.Combine("user", "sptappdata");
    public static readonly List<string> ArchiveFileInfoToIgnore =
    [
        "bepinex",
        Path.Combine("bepinex", "plugins"),
        "spt",
        Path.Combine("spt", "user"),
        Path.Combine("spt", "user", "mods")
    ];
}
