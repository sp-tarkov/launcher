using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class StateHelper
{
    private readonly LogHelper _logHelper;
    private readonly NavigationHelper _navigationHelper;
    public int? CurrentPagination;
    public Dictionary<string, SPTMod> ModList = [];
    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public StateHelper(
        LogHelper logHelper,
        NavigationHelper navigationHelper
    )
    {
        _logHelper = logHelper;
        _navigationHelper = navigationHelper;
    }

    public void LogoutAndDispose()
    {
        _logHelper.LogInfo($"Logged out of server {SelectedServer?.IpAddress ?? "Unknown"} and disposed");
        _navigationHelper.SetProfilesPages(false);
        _navigationHelper.SetProfilePages(false);
        ProfileTypes = new Dictionary<string, string>();
        ProfileList = [];
        ModList = [];
        SelectedProfile = null;
        SelectedServer = null;
    }

    public void SetSelectedServer(Server server)
    {
        SelectedServer = server;
    }

    public void SetSelectedProfile(MiniProfile miniProfile)
    {
        SelectedProfile = miniProfile;
    }
}
