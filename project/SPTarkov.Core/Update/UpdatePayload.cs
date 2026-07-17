namespace SPTarkov.Core.Update;

/// <summary>The entries an update payload may contain: launcher executables and files under <c>SPT_Data/Launcher</c>.</summary>
public static class UpdatePayload
{
    public const string WindowsExe = "SPT.Launcher.exe";

    public const string LinuxExe = "SPT.Launcher.Linux";

    public static List<string> DisallowedEntries(IEnumerable<string> entries)
    {
        return entries.Where(entry => !EntryAllowed(entry)).ToList();
    }

    private static bool EntryAllowed(string raw)
    {
        var entry = raw.Replace('\\', '/').Trim().Trim('/');

        if (entry.Length == 0 || entry.Contains(".."))
        {
            return false;
        }

        return entry.Equals(WindowsExe, StringComparison.OrdinalIgnoreCase)
            || entry.Equals(LinuxExe, StringComparison.OrdinalIgnoreCase)
            || entry.Equals("SPT_Data", StringComparison.OrdinalIgnoreCase)
            || entry.Equals("SPT_Data/Launcher", StringComparison.OrdinalIgnoreCase)
            || entry.StartsWith("SPT_Data/Launcher/", StringComparison.OrdinalIgnoreCase);
    }
}
