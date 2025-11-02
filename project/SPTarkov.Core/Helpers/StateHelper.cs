using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Spt;

namespace SPTarkov.Core.Helpers;

public class StateHelper
{
    private readonly ILogger<StateHelper> _logger;
    public Dictionary<string, SptMod> ModList = [];
    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public int CurrentPagination = 1;
    public string CurrentSearch = "";
    public string CurrentSort = "-downloads";
    public string CurrentFilter = "Include";
    public string CurrentAi = "Exclude";

    public bool AllowNavigation { get; set; } = true;
    public bool ShowDownloads { get; set; } = false;
    public event Action? OnStateChanged;

    public StateHelper(
        ILogger<StateHelper> logger
    )
    {
        _logger = logger;
    }

    public void LogoutAndDispose()
    {
        _logger.LogInformation("Logged out of server {SelectedServerIpAddress} and disposed", SelectedServer?.IpAddress ?? "Unknown");
        ProfileTypes = new Dictionary<string, string>();
        ProfileList = [];
        ModList = [];
        SelectedProfile = null;
        SelectedServer = null;
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

    public void SetHasDownloads()
    {
        NotifyStateChanged();
    }

    public void SetShowDownloads(bool state)
    {
        ShowDownloads = state;
        NotifyStateChanged();
    }
}
