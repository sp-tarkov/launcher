namespace SPTarkov.Core.SPT;

public class Paths
{
    public static readonly string ModCache = Path.Combine(Directory.GetCurrentDirectory(), @"user\Launcher\ModCache");

    public static readonly string UnZipped = Path.Combine(Directory.GetCurrentDirectory(), @"user\Launcher\UnZipped");

    // must contain \\ for windows reg key when looking on linux/wine
    public static readonly string UninstallEftRegKey =
        @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov";

    public static readonly string ProtonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        @".local/share/Steam/compatibilitytools.d");

    public static readonly string SevenZipDllPath = Path.Combine(Directory.GetCurrentDirectory(), @"SPT_Data\Launcher\Dependency\7z.dll");
    public static readonly string PatchPath = @"SPT\SPT_Data\Launcher\Patches";
    public static readonly string CoreDllPath = @"BepInEx\plugins\spt\spt-core.dll";
    public static readonly string HwechoDllPath = @"EscapeFromTarkov_Data\Plugins\x86_64\hwecho.dll";
    public static readonly string SptRegJson = @"user\sptRegistry\registry.json";
    public static readonly string LocalesPath = Path.Combine(Directory.GetCurrentDirectory(), @"SPT_Data\Launcher\Locales");
    public static readonly string LauncherAssetsPath = Path.Combine(Environment.CurrentDirectory, @"user\Launcher");
    public static readonly string LauncherSettingsPath = Path.Combine(Environment.CurrentDirectory, @"user\Launcher\LauncherSettings.json");
    public static readonly string EFTSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Battlestate Games\Escape From Tarkov\Settings"); // might have to change if BSG support linux?
    public static readonly string SPTSettingsFolder = @"user\sptsettings";
    public static readonly string SPTAppDataFolder = @"user\sptappdata";
}
