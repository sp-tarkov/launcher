namespace SPTarkov.Core.Helpers;

public static class FilePickerHelper
{
    /// <summary>
    /// Folder a picker should open at for the given path. Returns null if nothing usable exists, as Photino hands this
    /// straight to the platform dialog.
    /// </summary>
    public static string? StartDirectory(string? currentPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return null;
        }

        if (Directory.Exists(currentPath))
        {
            return currentPath;
        }

        var parent = Path.GetDirectoryName(currentPath);

        return Directory.Exists(parent) ? parent : null;
    }
}
