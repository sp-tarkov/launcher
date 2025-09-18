using SPT.Launcher.Controllers;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Splat;
using System;
using System.Reflection;

namespace SPT.Launcher
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) 
        {
            try
            {
                BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());

            return AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }
    }
}
