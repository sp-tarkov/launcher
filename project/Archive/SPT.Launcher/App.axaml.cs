using SPT.Launcher.Controllers;
using SPT.Launcher.ViewModels;
using SPT.Launcher.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Reactive;

namespace SPT.Launcher
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            RxApp.DefaultExceptionHandler = Observer.Create<Exception>((exception) =>
            {
                LogManager.Instance.Exception(exception);
            });
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
