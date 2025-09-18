using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using SPT.Launcher.Models.Launcher;

namespace SPT.Launcher.CustomControls;

public partial class GameLaunchBar : UserControl
{
    public GameLaunchBar()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<ProfileInfo> ProfileInfoProperty = AvaloniaProperty.Register<GameLaunchBar, ProfileInfo>(
        "ProfileInfo");

    public ProfileInfo ProfileInfo
    {
        get => GetValue(ProfileInfoProperty);
        set => SetValue(ProfileInfoProperty, value);
    }

    public static readonly StyledProperty<ICommand> StartGameCommandProperty = AvaloniaProperty.Register<GameLaunchBar, ICommand>(
        "StartGameCommand");

    public ICommand StartGameCommand
    {
        get => GetValue(StartGameCommandProperty);
        set => SetValue(StartGameCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> LogoutCommandProperty = AvaloniaProperty.Register<GameLaunchBar, ICommand>(
        "LogoutCommand");

    public ICommand LogoutCommand
    {
        get => GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }
}