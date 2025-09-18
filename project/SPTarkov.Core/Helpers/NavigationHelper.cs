namespace SPTarkov.Core.Helpers;

public class NavigationHelper
{
    public NavigationHelper(ConfigHelper configHelper)
    {
        if (configHelper.GetConfig().DebugSettings.DebugUser && configHelper.GetConfig().DebugSettings.ShowLoggingPage)
        {
            SetLoggingPages(true);
        }

        if (configHelper.GetConfig().UseBackground)
        {
            SetBackground(true);
        }
    }

    public event Action? OnStateChanged;
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public bool ShowProfilesPage { get; set; }
    public bool ShowProfilePage { get; set; }
    public bool ShowLoggingPage { get; set; }
    public bool ShowBackground { get; set; }

    public void SetProfilesPages(bool state)
    {
        ShowProfilesPage = state;
        NotifyStateChanged();
    }

    public void SetProfilePages(bool state)
    {
        ShowProfilePage = state;
        NotifyStateChanged();
    }

    public void SetLoggingPages(bool state)
    {
        ShowLoggingPage = state;
        NotifyStateChanged();
    }

    public void SetBackground(bool state)
    {
        ShowBackground = state;
        NotifyStateChanged();
    }
}
