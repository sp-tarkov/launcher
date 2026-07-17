using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Helpers;

public class StateHelper(ILogger<StateHelper> logger)
{
    private const int ModPagesPanelCloseDelayMs = 300;
    private long _modPagesPanelHoverVersion;

    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public List<ModPage> ModPages = [];
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public int CurrentPagination = 1;
    public string CurrentSearch = "";
    public string CurrentSort = "-downloads";
    public string CurrentFilter = "Include";
    public string CurrentAi = "Exclude";
    public string CurrentCategory = "all-cat";

    public List<ForgeCategory>? ListOfCategoriesAvailable;

    public bool AllowNavigation { get; set; } = true;
    public bool AllowServerPage { get; set; }
    public bool ModPagesPanelOpen { get; set; }

    public bool AutoConnectAttempted { get; set; } // Startup auto-connect guard

    public event Action? OnStateChanged;

    public void LogoutAndDispose()
    {
        logger.LogInformation("Logged out of server {SelectedServerIpAddress} and disposed", SelectedServer?.IpAddress ?? "Unknown");
        ProfileTypes = new Dictionary<string, string>();
        ProfileList = [];
        ModPages = [];
        SelectedProfile = null;
        SelectedServer = null;
        ModPagesPanelOpen = false;
    }

    public void SetSelectedServer(Server? server)
    {
        SelectedServer = server;
    }

    public void SetSelectedProfile(MiniProfile? miniProfile)
    {
        SelectedProfile = miniProfile;
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public void SetBackground()
    {
        NotifyStateChanged();
    }

    public void SetAllowNavigation(bool state)
    {
        AllowNavigation = state;
        NotifyStateChanged();
    }

    public void SetAllowServerPage(bool state)
    {
        AllowServerPage = state;
        NotifyStateChanged();
    }

    public void SetModPagesPanelOpen(bool state)
    {
        if (ModPagesPanelOpen == state)
        {
            return;
        }

        ModPagesPanelOpen = state;
        NotifyStateChanged();
    }

    /// <summary>Keeps the mod-pages panel open while the nav tile or the panel itself is hovered.</summary>
    public void HoldModPagesPanel()
    {
        _modPagesPanelHoverVersion++;
        SetModPagesPanelOpen(true);
    }

    /// <summary>Closes the mod-pages panel after a short delay, unless another hover has held it open since.</summary>
    public async Task ReleaseModPagesPanel()
    {
        var version = _modPagesPanelHoverVersion;
        await Task.Delay(ModPagesPanelCloseDelayMs);

        if (version == _modPagesPanelHoverVersion)
        {
            SetModPagesPanelOpen(false);
        }
    }
}
