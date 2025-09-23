using Microsoft.Extensions.Logging;
using SPTarkov.Core.Logging;
using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class StateHelper
{
    private readonly ILogger<StateHelper> _logger;
    public int? CurrentPagination;
    public Dictionary<string, SPTMod> ModList = [];
    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public bool AllowNavigation { get; set; } = true;
    public bool ShowBackground { get; set; }
    public event Action? OnStateChanged;

    public StateHelper(
        ILogger<StateHelper> logger,
        ConfigHelper configHelper
    )
    {
        _logger = logger;

        if (configHelper.GetConfig().UseBackground)
        {
            SetBackground(true);
        }
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

    public void SetBackground(bool state)
    {
        ShowBackground = state;
        NotifyStateChanged();
    }

    public void SetAllowNavigation(bool state)
    {
        AllowNavigation = state;
        NotifyStateChanged();
    }
}
