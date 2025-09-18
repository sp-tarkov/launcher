using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using SPT.Launcher.Models.Launcher;

namespace SPT.Launcher.CustomControls;

public partial class ProfileCard : UserControl
{
    public ProfileCard()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<ProfileInfo> ProfileInfoProperty = AvaloniaProperty.Register<ProfileCard, ProfileInfo>(
        "ProfileInfo");

    public ProfileInfo ProfileInfo
    {
        get => GetValue(ProfileInfoProperty);
        set => SetValue(ProfileInfoProperty, value);
    }

    public static readonly StyledProperty<string> CurrentIdProperty = AvaloniaProperty.Register<ProfileCard, string>(
        "CurrentId");

    public string CurrentId
    {
        get => GetValue(CurrentIdProperty);
        set => SetValue(CurrentIdProperty, value);
    }

    public static readonly StyledProperty<string> CurrentEditionProperty = AvaloniaProperty.Register<ProfileCard, string>(
        "CurrentEdition");

    public string CurrentEdition
    {
        get => GetValue(CurrentEditionProperty);
        set => SetValue(CurrentEditionProperty, value);
    }

    public static readonly StyledProperty<bool> WipeProfileOnStartProperty = AvaloniaProperty.Register<ProfileCard, bool>(
        "WipeProfileOnStart");

    public bool WipeProfileOnStart
    {
        get => GetValue(WipeProfileOnStartProperty);
        set => SetValue(WipeProfileOnStartProperty, value);
    }

    public static readonly StyledProperty<bool> ProfileWipePendingProperty = AvaloniaProperty.Register<ProfileCard, bool>(
        "ProfileWipePending");

    public bool ProfileWipePending
    {
        get => GetValue(ProfileWipePendingProperty);
        set => SetValue(ProfileWipePendingProperty, value);
    }

    public static readonly StyledProperty<ICommand> RemoveProfileCommandProperty = AvaloniaProperty.Register<ProfileCard, ICommand>(
        "RemoveProfileCommand");

    public ICommand RemoveProfileCommand
    {
        get => GetValue(RemoveProfileCommandProperty);
        set => SetValue(RemoveProfileCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> CopyCommandProperty = AvaloniaProperty.Register<ProfileCard, ICommand>(
        "CopyCommand");

    public ICommand CopyCommand
    {
        get => GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> ChangeEditionCommandProperty = AvaloniaProperty.Register<ProfileCard, ICommand>(
        "ChangeEditionCommand");

    public ICommand ChangeEditionCommand
    {
        get => GetValue(ChangeEditionCommandProperty);
        set => SetValue(ChangeEditionCommandProperty, value);
    }
}