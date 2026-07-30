using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Helpers;
using SPTarkov.Launcher.Platform;

namespace SPTarkov.Launcher.Helpers;

/// <summary>
/// Windows "Close To Tray" support. Owns the tray icon lifecycle and exposes a small contract that Launcher wires to the window operations.
/// </summary>
/// <remarks>
/// The icon and its native context menu are provided by TrayIcon. Menu clicks fire on the tray's background thread. Launcher's restore/exit
/// callbacks marshal onto the Photino UI thread themselves.
/// </remarks>
public sealed class TrayHelper(LocaleHelper localeHelper, StateHelper stateHelper, ILogger<TrayHelper> logger) : IDisposable
{
    private TrayIcon? _trayIcon;
    private string? _iconPath;
    private Action? _onRestore;
    private Action? _onExit;

    public bool IsShowing { get; private set; }

    /// <summary>Stores the tray icon path and the actions run when the user picks "Show"/"Exit" (or clicks the icon).</summary>
    public void Configure(string iconPath, Action onRestore, Action onExit)
    {
        _iconPath = iconPath;
        _onRestore = onRestore;
        _onExit = onExit;
    }

    /// <summary>Shows the tray icon, creating it on first use.</summary>
    public void Show()
    {
        if (!OperatingSystem.IsWindows() || _iconPath is null)
        {
            return;
        }

        try
        {
            _trayIcon ??= new TrayIcon(_iconPath, TitleHelper.AppName, BuildMenu, onActivate: () => _onRestore?.Invoke(), logger);
            _trayIcon.Show();
            IsShowing = true;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to show tray icon: {ex}", ex);
        }
    }

    /// <summary>
    /// Builds the tray context menu from live state; TrayIcon invokes this on every open, so the menu reflects the current server
    /// connection and its mod pages. Runs on the tray's background thread.
    /// </summary>
    private IReadOnlyList<TrayIcon.MenuItem> BuildMenu()
    {
        var items = new List<TrayIcon.MenuItem> { new(localeHelper.Get("tray_show"), () => _onRestore?.Invoke()) };

        // While connected to a server, nest its SIC homepage and any mod-registered pages under it.
        if (stateHelper.SelectedServer is { } server)
        {
            var serverItems = new List<TrayIcon.MenuItem>
            {
                new(localeHelper.Get("tray_sic_homepage"), () => OpenUrl($"https://{server.IpAddress}/")),
            };

            if (stateHelper.ModPages.Count > 0)
            {
                serverItems.Add(TrayIcon.MenuItem.Separator);
                foreach (var page in stateHelper.ModPages)
                {
                    var url = $"https://{server.IpAddress}{page.HomePage}";
                    serverItems.Add(new TrayIcon.MenuItem(page.Name, () => OpenUrl(url)));
                }
            }

            items.Add(TrayIcon.MenuItem.Separator);
            items.Add(TrayIcon.MenuItem.Submenu(string.Format(localeHelper.Get("tray_server_pages"), server.Name), serverItems));
        }

        items.Add(TrayIcon.MenuItem.Separator);
        items.Add(new TrayIcon.MenuItem(localeHelper.Get("tray_exit"), () => _onExit?.Invoke()));

        return items;
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to open URL from tray: {ex}", ex);
        }
    }

    /// <summary>Hides the tray icon (keeps it alive for reuse). Thread-safe.</summary>
    public void Hide()
    {
        _trayIcon?.Hide();
        IsShowing = false;
    }

    /// <summary>Tears down the tray icon. Called at app shutdown from the main thread.</summary>
    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        IsShowing = false;
    }
}
