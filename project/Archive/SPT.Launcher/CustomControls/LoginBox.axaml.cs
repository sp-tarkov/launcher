using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace SPT.Launcher.CustomControls;

public partial class LoginBox : UserControl
{
    public LoginBox()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<string> UsernameProperty = AvaloniaProperty.Register<LoginBox, string>(
        "Username");

    public string Username
    {
        get => GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public static readonly StyledProperty<ICommand> LoginCommandProperty = AvaloniaProperty.Register<LoginBox, ICommand>(
        "LoginCommand");

    public ICommand LoginCommand
    {
        get => GetValue(LoginCommandProperty);
        set => SetValue(LoginCommandProperty, value);
    }

    public static readonly StyledProperty<bool> IsLoggedInProperty = AvaloniaProperty.Register<LoginBox, bool>(
        "IsLoggedIn");

    public bool IsLoggedIn
    {
        get => GetValue(IsLoggedInProperty);
        set => SetValue(IsLoggedInProperty, value);
    }
}