using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SPTarkov.Launcher.Platform;

/// <summary>
/// Opens URLs in the user's system browser. Injected into Razor components to open a link directly, and exposed to JS via a
/// DotNetObjectReference so intercepted external anchor clicks can be routed here.
/// </summary>
public sealed class BrowserBridge(ILogger<BrowserBridge> logger)
{
    /// <summary>Opens url in the system browser. Callable from JavaScript as well as directly from C#.</summary>
    [JSInvokable]
    public void OpenExternal(string url)
    {
        if (!IsWebUrl(url))
        {
            logger.LogWarning("Refused to open {Url}: only http and https are opened externally.", url);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to open external URL {Url}: {Ex}", url, ex);
        }
    }

    private static bool IsWebUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
