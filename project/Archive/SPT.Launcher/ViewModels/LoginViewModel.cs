using SPT.Launcher.Attributes;
using SPT.Launcher.Helpers;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models;
using SPT.Launcher.Models.SPT;
using SPT.Launcher.Models.Launcher;
using SPT.Launcher.ViewModels.Dialogs;
using Avalonia.Controls.Notifications;
using ReactiveUI;
using Splat;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SPT.Launcher.ViewModels
{
    [RequireServerConnected]
    public class LoginViewModel(IScreen Host, bool NoAutoLogin = false) : ViewModelBase(Host)
    {
        public ObservableCollection<ProfileInfo> ExistingProfiles { get; set; } = new ObservableCollection<ProfileInfo>();

        public LoginModel Login { get; set; } = new LoginModel();

        public ReactiveCommand<Unit, Unit>? LoginCommand { get; set; }

        public override async Task OnCreateAsync()
        {
            //setup reactive commands
            LoginCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                AccountStatus status = await AccountManager.LoginAsync(Login);

                switch (status)
                {
                    case AccountStatus.OK:
                        {
                            if (LauncherSettingsProvider.Instance.UseAutoLogin && LauncherSettingsProvider.Instance.Server.AutoLoginCreds != Login)
                            {
                                LauncherSettingsProvider.Instance.Server.AutoLoginCreds = Login;
                            }

                            LauncherSettingsProvider.Instance.SaveSettings();
                            ViewModelBase vm = new ProfileViewModel(HostScreen);
                            await vm.OnCreateAsync();

                            await NavigateTo(vm);
                            break;
                        }
                    case AccountStatus.LoginFailed:
                        {
                            // Create account if it doesn't exist
                            if (!string.IsNullOrWhiteSpace(Login.Username))
                            {
                                if (Login.Username.Length > 15)
                                {
                                    SendNotification(LocalizationProvider.Instance.registration_failed, LocalizationProvider.Instance.register_failed_name_limit, NotificationType.Error);
                                    return;
                                }

                                var result = await ShowDialog(new RegisterDialogViewModel(null, Login.Username));

                                if (result != null && result is SPTEdition edition)
                                {
                                    AccountStatus registerResult = await AccountManager.RegisterAsync(Login.Username, Login.Password, edition.Name);

                                    switch (registerResult)
                                    {
                                        case AccountStatus.OK:
                                            {
                                                if (LauncherSettingsProvider.Instance.UseAutoLogin && LauncherSettingsProvider.Instance.Server.AutoLoginCreds != Login)
                                                {
                                                    LauncherSettingsProvider.Instance.Server.AutoLoginCreds = Login;
                                                }

                                                LauncherSettingsProvider.Instance.SaveSettings();
                                                SendNotification(LocalizationProvider.Instance.profile_created, Login.Username, NotificationType.Success);

                                                ViewModelBase vm = new ProfileViewModel(HostScreen);
                                                await vm.OnCreateAsync();

                                                await NavigateTo(vm);
                                                break;
                                            }
                                        case AccountStatus.RegisterFailed:
                                            {
                                                SendNotification("", LocalizationProvider.Instance.registration_failed, NotificationType.Error);
                                                break;
                                            }
                                        case AccountStatus.NoConnection:
                                            {
                                                await NavigateTo(new ConnectServerViewModel(HostScreen));
                                                break;
                                            }
                                        default:
                                            {
                                                SendNotification("", registerResult.ToString(), NotificationType.Error);
                                                break;
                                            }
                                    }

                                    return;
                                }
                            }

                            SendNotification("", LocalizationProvider.Instance.login_failed, NotificationType.Error);

                            break;
                        }
                    case AccountStatus.NoConnection:
                        {
                            await NavigateTo(new ConnectServerViewModel(HostScreen));
                            break;
                        }
                }
            });

            //cache and touch background image
            var backgroundImage = Locator.Current.GetService<ImageHelper>("bgimage");

            if (backgroundImage is not null)
            {
                await ImageRequest.CacheBackgroundImage();

                backgroundImage.Touch();
            }

            //handle auto-login
            if (LauncherSettingsProvider.Instance.UseAutoLogin && LauncherSettingsProvider.Instance.Server.AutoLoginCreds != null && !NoAutoLogin)
            {
                Login = LauncherSettingsProvider.Instance.Server.AutoLoginCreds;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoginCommand.Execute();
                });
                return;
            }

            await GetExistingProfiles();
        }

        public void LoginProfileCommand(object parameter)
        {
            if (parameter == null) return;

            Task.Run(() =>
            {
                if (parameter is string username)
                {
                    Login.Username = username;

                    LoginCommand?.Execute();
                }
            });
        }

        public async Task GetExistingProfiles()
        {
            ExistingProfiles.Clear();

            ServerProfileInfo[] existingProfiles = await AccountManager.GetExistingProfilesAsync();

            if (existingProfiles != null)
            {
                foreach(ServerProfileInfo profile in existingProfiles)
                {
                    ProfileInfo profileInfo = new ProfileInfo(profile);

                    ExistingProfiles.Add(profileInfo);

                    await ImageRequest.CacheSideImage(profileInfo.Side);

                    ImageHelper sideImage = new ImageHelper() { Path = profileInfo.SideImage };
                    sideImage.Touch();

                    await Task.Delay(100);
                }
            }
        }
    }
}
