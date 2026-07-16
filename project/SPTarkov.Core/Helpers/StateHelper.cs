using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Helpers;

public class StateHelper(ILogger<StateHelper> logger)
{
    public List<MiniProfile> ProfileList = [];
    public Dictionary<string, string> ProfileTypes = new();
    public List<ModPage> ModPages = [];
    public MiniProfile? SelectedProfile;
    public Server? SelectedServer;

    public const string DefaultSearch = "";
    public const string DefaultFilter = "Include";
    public const string DefaultAi = "Exclude";
    public const string DefaultCategory = "all-cat";

    public int CurrentPagination = 1;
    public string CurrentSearch = DefaultSearch;
    public string CurrentSort = "-downloads";
    public string CurrentFilter = DefaultFilter;
    public string CurrentAi = DefaultAi;
    public string CurrentCategory = DefaultCategory;

    public bool HasNonDefaultForgeFilters =>
        CurrentSearch != DefaultSearch || CurrentFilter != DefaultFilter || CurrentAi != DefaultAi || CurrentCategory != DefaultCategory;

    public List<ForgeCategory>? ListOfCategoriesAvailable;

    public bool AllowNavigation { get; set; } = true;
    public bool AllowServerPage { get; set; } = false;

    public bool AutoConnectAttempted { get; set; } = false; // Startup auto-connect guard

    public event Action? OnStateChanged;

    public void LogoutAndDispose()
    {
        logger.LogInformation("Logged out of server {SelectedServerIpAddress} and disposed", SelectedServer?.IpAddress ?? "Unknown");
        ProfileTypes = new Dictionary<string, string>();
        ProfileList = [];
        ModPages = [];
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

    public void SetAllowServerPage(bool state)
    {
        AllowServerPage = state;
        NotifyStateChanged();
    }

    public void ResetForgeFilters()
    {
        CurrentSearch = DefaultSearch;
        CurrentFilter = DefaultFilter;
        CurrentAi = DefaultAi;
        CurrentCategory = DefaultCategory;
    }
}
