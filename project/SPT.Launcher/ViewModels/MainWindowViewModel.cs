using Avalonia;
using ReactiveUI;
using System.Reactive.Disposables;
using SPT.Launcher.Models;
using SPT.Launcher.MiniCommon;
using System.IO;
using Splat;
using SPT.Launcher.Models.SPT;
using SPT.Launcher.Helpers;
using SPT.Launcher.ViewModels.Dialogs;
using Avalonia.Threading;
using DialogHostAvalonia;


namespace SPT.Launcher.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IActivatableViewModel, IScreen
    {
        public SPTVersion VersionInfo { get; set; } = new SPTVersion();
        public RoutingState Router { get; } = new RoutingState();
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ImageHelper Background { get; } = new ImageHelper()
        {
            Path = Path.Join(ImageRequest.ImageCacheFolder, "bg.png")
        };

        public MainWindowViewModel()
        {
            Locator.CurrentMutable.RegisterConstant<ImageHelper>(Background, "bgimage");

            Locator.CurrentMutable.RegisterConstant<SPTVersion>(VersionInfo, "sptversion");
            
            LauncherSettingsProvider.Instance.ResetDefaults();

            LauncherSettingsProvider.Instance.AllowSettings = true;

            if (LauncherSettingsProvider.Instance.FirstRun)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    LauncherSettingsProvider.Instance.FirstRun = false;

                    LocalizationProvider.TryAutoSetLocale();

                    var viewModel = new ConfirmationDialogViewModel(this,
                        LocalizationProvider.Instance.copy_live_settings_question,
                        LocalizationProvider.Instance.yes,
                        LocalizationProvider.Instance.no);

                    var confirmCopySettings = await DialogHost.Show(viewModel);

                    if (confirmCopySettings is bool and true)
                    {
                        var settingsVm = new SettingsViewModel(this);

                        await settingsVm.ResetGameSettingsCommand();
                    }
                    
                    LauncherSettingsProvider.Instance.SaveSettings();
                });
            }

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                Router.Navigate.Execute(new ConnectServerViewModel(this));
            });
        }

        public void CloseCommand()
        {
            if (Application.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktopApp)
            {
                desktopApp.MainWindow.Close();
            }
        }

        public void MinimizeCommand()
        {
            if (Application.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktopApp)
            {
                desktopApp.MainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
            }
        }

        public void GoToSettingsCommand()
        {
            LauncherSettingsProvider.Instance.AllowSettings = false;

            Router.Navigate.Execute(new SettingsViewModel(this));
        }
    }
}
