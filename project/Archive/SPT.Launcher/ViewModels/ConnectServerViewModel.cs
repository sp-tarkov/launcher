using ReactiveUI;
using Splat;
using SPT.Launcher.Controllers;
using SPT.Launcher.Helpers;
using SPT.Launcher.Models.Launcher;
using SPT.Launcher.Models.SPT;
using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace SPT.Launcher.ViewModels
{
    public class ConnectServerViewModel : ViewModelBase
    {
        private bool noAutoLogin = false;
        private bool listeningToChangingProperty = false;
        private CancellationTokenSource debounceTokenSource = new();

        public ConnectServerModel connectModel { get; set; } = new ConnectServerModel()
        {
            InfoText = LocalizationProvider.Instance.server_connecting
        };

        public ConnectServerViewModel(IScreen Host, bool NoAutoLogin = false) : base(Host)
        {
            noAutoLogin = NoAutoLogin;

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                Task.Run(async () =>
                {
                    await ConnectServer();
                });
            });
        }

        public async Task ConnectServer()
        {
            if (!listeningToChangingProperty)
            {
                LauncherSettingsProvider.Instance.Server.PropertyChanged += HandleUrlChangedAsync;
                listeningToChangingProperty = true;
            }

            if (!await ServerManager.LoadServerAsync(LauncherSettingsProvider.Instance.Server.Url))
            {
                connectModel.ConnectionFailed = true;
                connectModel.InfoText = string.Format(LocalizationProvider.Instance.server_unavailable_format_1,
                    LauncherSettingsProvider.Instance.Server.Name);

                return;
            }

            bool connected = await ServerManager.PingServerAsync();

            connectModel.ConnectionFailed = !connected;

            connectModel.InfoText = connected ? LocalizationProvider.Instance.ok : string.Format(LocalizationProvider.Instance.server_unavailable_format_1, LauncherSettingsProvider.Instance.Server.Name);

            if (connected)
            {
                SPTVersion version = Locator.Current.GetService<SPTVersion>("sptversion");

                version.ParseVersionInfo(await ServerManager.GetVersionAsync());

                LogManager.Instance.Info($"Connected to server: {ServerManager.SelectedServer.backendUrl} - SPT MatchingVersion: {version}");

                ViewModelBase vm = new LoginViewModel(HostScreen, noAutoLogin);

                await vm.OnCreateAsync();

                await NavigateTo(vm);
            }

            LauncherSettingsProvider.Instance.AllowSettings = true;
            LauncherSettingsProvider.Instance.Server.PropertyChanged -= HandleUrlChangedAsync;
            listeningToChangingProperty = false;
        }

        public void RetryCommand()
        {
            connectModel.InfoText = LocalizationProvider.Instance.server_connecting;

            connectModel.ConnectionFailed = false;

            Task.Run(async () =>
            {
                await ConnectServer();
            });
        }

        private async void HandleUrlChangedAsync(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LauncherSettingsProvider.Instance.Server.Url))
            {
                debounceTokenSource.Cancel();
                debounceTokenSource = new CancellationTokenSource();

                try
                {
                    await Task.Delay(600, debounceTokenSource.Token);

                    //Cancel all current requests at this stage
                    RequestHandler.CancelCurrentRequests();
                }
                catch (OperationCanceledException)
                { }
            }
        }
    }
}
