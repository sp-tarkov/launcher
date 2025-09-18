using SPT.Launcher.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SPT.Launcher.Views;

public partial class ModInfoView : ReactiveUserControl<ModInfoViewModel>
{
    public ModInfoView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}