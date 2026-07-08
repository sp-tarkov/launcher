using SPTarkov.Core.Helpers;

namespace SPTarkov.Launcher.Helpers;

public class TitleHelper(LocaleHelper localeHelper, StateHelper stateHelper)
{
    public const string AppName = "SPT Launcher";
    private const string Separator = "-";

    public void Apply(string relativePath, string? dynamicLeaf = null)
    {
        var segments = BuildSegments(relativePath, dynamicLeaf);
        if (segments is null)
        {
            // Redirect or unmapped route
            return;
        }

        var title = AppName;
        foreach (var segment in segments)
        {
            title += $" {Separator} {segment}";
        }

        Launcher.App.MainWindow.SetTitle(title);
    }

    private List<string>? BuildSegments(string relativePath, string? dynamicLeaf)
    {
        var key = relativePath.Split('?')[0].Trim('/').ToLowerInvariant();

        // Home is server-selection
        if (key.Length == 0)
        {
            return [localeHelper.Get("servers")];
        }

        switch (key)
        {
            case "profiles":
                return WithServerContext(localeHelper.Get("profiles"));

            case "profile":
                return WithServerContext(ProfileLabel());

            case "settings":
                return [localeHelper.Get("settings")];

            case "info":
                return [localeHelper.Get("info")];

            case "forge":
                return ["Forge"];

            case "modloader":
                return [localeHelper.Get("mod_loader")];
        }

        if (key.StartsWith("forgemod"))
        {
            return dynamicLeaf is { Length: > 0 } ? ["Forge", dynamicLeaf] : ["Forge"];
        }

        return null;
    }

    private List<string> WithServerContext(params string?[] trailing)
    {
        var segments = new List<string>();

        var serverName = stateHelper.SelectedServer?.Name;
        if (!string.IsNullOrWhiteSpace(serverName))
        {
            segments.Add($"{localeHelper.Get("server")}: {serverName}");
        }

        segments.AddRange(trailing.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!));
        return segments;
    }

    private string? ProfileLabel()
    {
        var username = stateHelper.SelectedProfile?.Username;
        return string.IsNullOrWhiteSpace(username) ? null : $"{localeHelper.Get("profile")}: {username}";
    }
}
