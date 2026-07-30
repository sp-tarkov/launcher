namespace SPTarkov.Core.SPT;

public class Paths
{
    // Registry paths must contain "\\" for cross-compatability
    public static readonly string UninstallEftRegKey = @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov";
    public static readonly string SteamRegistryInstallKey = @"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 3932890";

    // The launcher runs from inside the SPT_Runtime folder, so the current working directory is the runtime root.
    private static readonly string _runtimeRoot = Directory.GetCurrentDirectory();

    private static readonly string _userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string _applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static readonly string ProtonPath = Path.Join(_userProfile, ".local", "share", "Steam", "compatibilitytools.d");
    public static readonly string SevenZip = Path.Join(_runtimeRoot, "SPT_Data", "Launcher", "Dependency");
    public static readonly string PatchPath = Path.Join(_runtimeRoot, "SPT_Data", "Launcher", "Patches");
    public static readonly string CoreDllPath = Path.Join("BepInEx", "plugins", "spt", "spt-core.dll");
    public static readonly string HwechoDllPath = Path.Join("EscapeFromTarkov_Data", "Plugins", "x86_64", "hwecho.dll");
    public static readonly string SptRegJson = Path.Join(_runtimeRoot, "user", "sptRegistry", "registry.json");
    public static readonly string LocalesPath = Path.Join(_runtimeRoot, "SPT_Data", "Launcher", "Locales");
    public static readonly string LauncherAssetsPath = Path.Join(_runtimeRoot, "user", "Launcher");
    public static readonly string LauncherSettingsPath = Path.Join(_runtimeRoot, "user", "Launcher", "LauncherSettings.json");
    public static readonly string EftSettingsFolder = Path.Join(_applicationData, "Battlestate Games", "Escape From Tarkov", "Settings");
    public static readonly string SptSettingsFolder = Path.Join(_runtimeRoot, "user", "sptsettings");
    public static readonly string SptAppDataFolder = Path.Join(_runtimeRoot, "user", "sptappdata");
    public static readonly string LogsFolder = Path.Join(_runtimeRoot, "user", "logs");
    public static readonly string LauncherLog = Path.Join(_runtimeRoot, "user", "logs", "Launcher.log");
    public static readonly string ProfilesFolder = Path.Join(_runtimeRoot, "user", "profiles");
}
