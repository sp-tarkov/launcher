using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class StateHelper
{
    private readonly LogHelper _logHelper;
    public int? CurrentPagination;
    public Dictionary<string, SPTMod> ModList = [];
    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public StateHelper(
        LogHelper logHelper,
        ConfigHelper configHelper
    )
    {
        _logHelper = logHelper;

        if (configHelper.GetConfig().UseBackground)
        {
            SetBackground(true);
        }
    }

    public void LogoutAndDispose()
    {
        _logHelper.LogInfo($"Logged out of server {SelectedServer?.IpAddress ?? "Unknown"} and disposed");
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

    public event Action? OnStateChanged;
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
    public bool ShowBackground { get; set; }

    public void SetBackground(bool state)
    {
        ShowBackground = state;
        NotifyStateChanged();
    }
}
