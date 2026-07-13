namespace SPTarkov.Core.Update;

internal static class UpdateLayout
{
    public const string BackupSuffix = ".spt-bak";

    public static string InstallRoot { get; } = Path.GetDirectoryName(Environment.ProcessPath) ?? Directory.GetCurrentDirectory();

    public static string Staging { get; } = Path.Combine(InstallRoot, ".update");

    public static string StagingDownload { get; } = Path.Combine(Staging, "payload.zip");

    public static string StagingExtract { get; } = Path.Combine(Staging, "extracted");

    public static string StateFile { get; } = Path.Combine(Staging, "state.json");

    public static string RunningExeName { get; } = OperatingSystem.IsWindows() ? UpdatePayload.WindowsExe : UpdatePayload.LinuxExe;

    public static string DormantExeName { get; } = OperatingSystem.IsWindows() ? UpdatePayload.LinuxExe : UpdatePayload.WindowsExe;

    public static string RunningExePath { get; } = Path.Combine(InstallRoot, RunningExeName);

    public static string OldExePath { get; } = RunningExePath + ".old";

    public static string DataRoot { get; } = Path.Combine(InstallRoot, "SPT_Data", "Launcher");
}
