using SPT.Launcher.Controllers;
using SPT.Launcher.Helpers;
using SPT.Launcher.Models;
using SPT.Launcher.Models.Launcher;
using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace SPT.Launcher.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public LocaleCollection Locales { get; set; } = new LocaleCollection();

        private GameStarter gameStarter = new GameStarter(new GameStarterFrontend());

        public SettingsViewModel(IScreen Host) : base(Host)
        {
            if(Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.Closing += MainWindow_Closing;
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            LauncherSettingsProvider.Instance.SaveSettings();
        }

        public async Task CopyLogsToClipboard()
        {
            LogManager.Instance.Info("[Settings] Copying logs to clipboard ...");

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.Clipboard == null)
                {
                    LogManager.Instance.Error("[Settings] Failed to get clipboard");
                    return;
                }

                var filesToCopy = new List<string> { LogManager.Instance.LogFile };
                
                var serverLog = Path.Join(LauncherSettingsProvider.Instance.GamePath, @"\user\logs",
                    $"server-{DateTime.Now:yyyy-MM-dd}.log");
                var bepinexLog = Path.Join(LauncherSettingsProvider.Instance.GamePath, @"BepInEx\LogOutput.log");

                if (AccountManager.SelectedAccount?.id != null)
                {
                    filesToCopy.Add(Path.Join(LauncherSettingsProvider.Instance.GamePath, @"\user\profiles",
                        $"{AccountManager.SelectedAccount.id}.json"));
                }

                if (File.Exists(serverLog))
                {
                    filesToCopy.Add(serverLog);
                }

                if (File.Exists(bepinexLog))
                {
                    filesToCopy.Add(bepinexLog);
                }

                var logsPath = Path.Join(LauncherSettingsProvider.Instance.GamePath, "Logs");
                if (Directory.Exists(logsPath))
                {
                    var traceLogs = Directory.GetFiles(logsPath, $"{DateTime.Now:yyyy.MM.dd}_* traces.log",
                        SearchOption.AllDirectories);

                    var log = traceLogs.Length > 0 ? traceLogs[0] : "";

                    if (!string.IsNullOrWhiteSpace(log))
                    {
                        filesToCopy.Add(log);
                    }
                }
                
                List<IStorageFile> files = new List<IStorageFile>();

                foreach (var logPath in filesToCopy)
                {
                    var file = await desktop.MainWindow.StorageProvider.TryGetFileFromPathAsync(logPath);

                    if (file != null)
                    {
                        LogManager.Instance.Debug($"file to copy :: {logPath}");
                        files.Add(file);
                        continue;
                    }
                    
                    LogManager.Instance.Warning($"failed to get file to copy :: {logPath}");
                }

                if (files.Count == 0)
                {
                    LogManager.Instance.Warning("[Settings] Failed to copy log files");
                    SendNotification("", LocalizationProvider.Instance.copy_failed);
                }

                var data = new DataObject();

                data.Set(DataFormats.Files, files.ToArray());
                
                await desktop.MainWindow.Clipboard.SetDataObjectAsync(data);
                
                LogManager.Instance.Info($"[Settings] {files.Count} log/s copied to clipboard");
                SendNotification("", $"{files.Count} {LocalizationProvider.Instance.copied}");
            }
        }

        public void GoBackCommand()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.Closing -= MainWindow_Closing;
            }

            LauncherSettingsProvider.Instance.AllowSettings = true;

            if (!LauncherSettingsProvider.Instance.SaveSettings())
            {
                SendNotification("", LocalizationProvider.Instance.failed_to_save_settings, NotificationType.Error);
            }

            NavigateBack();
        }

        public void CleanTempFilesCommand()
        {
            LogManager.Instance.Info("[Settings] Clearing temp files ...");
            bool filesCleared = gameStarter.CleanTempFiles();

            if (filesCleared)
            {
                LogManager.Instance.Info("[Settings] Temp files cleared");
                SendNotification("", LocalizationProvider.Instance.clean_temp_files_succeeded, NotificationType.Success);
            }
            else
            {
                LogManager.Instance.Info("[Settings] Temp files failed to clear");
                SendNotification("", LocalizationProvider.Instance.clean_temp_files_failed, NotificationType.Error);
            }
        }

        public void RemoveRegistryKeysCommand()
        {
            LogManager.Instance.Info("[Settings] Removing registry keys ...");
            bool regKeysRemoved = gameStarter.RemoveRegistryKeys();

            if (regKeysRemoved)
            {
                LogManager.Instance.Info("[Settings] Registry keys removed");
                SendNotification("", LocalizationProvider.Instance.remove_registry_keys_succeeded, Avalonia.Controls.Notifications.NotificationType.Success);
            }
            else
            {
                LogManager.Instance.Info("[Settings] Registry keys failed to remove");
                SendNotification("", LocalizationProvider.Instance.remove_registry_keys_failed, Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }

        public async Task ResetGameSettingsCommand()
        {
            LogManager.Instance.Info("[Settings] Reseting game settings ...");
            string EFTSettingsFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Battlestate Games", "Escape from Tarkov", "Settings");
            string SPTSettingsFolder = Path.Join(LauncherSettingsProvider.Instance.GamePath, "user", "sptsettings");

            if (!Directory.Exists(EFTSettingsFolder))
            {
                LogManager.Instance.Warning($"[Settings] EFT settings folder not found, can't reset :: Path: {EFTSettingsFolder}");
                SendNotification("", LocalizationProvider.Instance.load_live_settings_failed, Avalonia.Controls.Notifications.NotificationType.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(SPTSettingsFolder);

                foreach (string dirPath in Directory.GetDirectories(EFTSettingsFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(EFTSettingsFolder, SPTSettingsFolder));
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(EFTSettingsFolder, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(EFTSettingsFolder, SPTSettingsFolder), true);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
                SendNotification("", LocalizationProvider.Instance.load_live_settings_failed, Avalonia.Controls.Notifications.NotificationType.Error);
                return;
            }
            
            LogManager.Instance.Info("[Settings] Game settings reset to live settings");
            SendNotification("", LocalizationProvider.Instance.load_live_settings_succeeded, Avalonia.Controls.Notifications.NotificationType.Success);
        }

        public async Task ClearGameSettingsCommand()
        {
            LogManager.Instance.Info("[Settings] Clearing game settings ...");
            var SPTSettingsDir = new DirectoryInfo(Path.Join(LauncherSettingsProvider.Instance.GamePath, "user", "sptsettings"));

            try
            {
                SPTSettingsDir.Delete(true);

                Directory.CreateDirectory(SPTSettingsDir.FullName);
            }
            catch(Exception ex)
            {
                LogManager.Instance.Exception(ex);
                SendNotification("", LocalizationProvider.Instance.clear_game_settings_failed, Avalonia.Controls.Notifications.NotificationType.Error);
                return;
            }
            
            LogManager.Instance.Info("[Settings] Game settings cleared");
            SendNotification("", LocalizationProvider.Instance.clear_game_settings_succeeded, Avalonia.Controls.Notifications.NotificationType.Success);
        }

        public void OpenGameFolderCommand()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.EndsInDirectorySeparator(LauncherSettingsProvider.Instance.GamePath) ? LauncherSettingsProvider.Instance.GamePath : LauncherSettingsProvider.Instance.GamePath + Path.DirectorySeparatorChar,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public async Task SelectGameFolderCommand()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var startPath = await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(Assembly.GetExecutingAssembly().Location);
                
                var dir = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Select your SPT folder",
                    SuggestedStartLocation = startPath
                });

                if (dir == null || dir.Count == 0)
                {
                    return;
                }

                LauncherSettingsProvider.Instance.GamePath = dir[0].Path.LocalPath;
            }
        }
    }
}
