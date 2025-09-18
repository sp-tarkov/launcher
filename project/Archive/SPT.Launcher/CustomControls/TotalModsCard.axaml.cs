using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SPT.Launcher.CustomControls;

public partial class TotalModsCard : UserControl
{
    public TotalModsCard()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<int> ActiveModsCountProperty = AvaloniaProperty.Register<TotalModsCard, int>(
        "ActiveModsCount");

    public int ActiveModsCount
    {
        get => GetValue(ActiveModsCountProperty);
        set => SetValue(ActiveModsCountProperty, value);
    }

    public static readonly StyledProperty<ICommand> OpenModsInfoCommandProperty = AvaloniaProperty.Register<TotalModsCard, ICommand>(
        "OpenModsInfoCommand");

    public ICommand OpenModsInfoCommand
    {
        get => GetValue(OpenModsInfoCommandProperty);
        set => SetValue(OpenModsInfoCommandProperty, value);
    }
}