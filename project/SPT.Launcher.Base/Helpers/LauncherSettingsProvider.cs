/* LauncherSettingsProvider.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.Launcher;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using SPT.Launcher.Utilities;
using SPT.Launcher.Controllers;

namespace SPT.Launcher.Helpers
{
    public static class LauncherSettingsProvider
    {
        public static string DefaultSettingsFileLocation = Path.Join(Environment.CurrentDirectory, "user", "launcher", "config.json");
        public static Settings Instance { get; } = Json.Load<Settings>(DefaultSettingsFileLocation) ?? new Settings();
    }

    public class Settings : NotifyPropertyChangedBase
    {
        public bool FirstRun { get; set; } = true;

        public bool SaveSettings()
        {
            Server.Url = Path.TrimEndingDirectorySeparator(Server.Url.Trim());
            return Json.SaveWithFormatting(LauncherSettingsProvider.DefaultSettingsFileLocation, this, Formatting.Indented);
        }

        public void ResetDefaults()
        {
            string defaultUrl = "https://127.0.0.1:6969";
            string defaultPath = Directory.GetParent(Environment.CurrentDirectory).FullName;
            
            // don't reset if running in dev mode
            if (IsDevMode)
            {
                LogManager.Instance.Info("Running in dev mode, not resetting");
                return;
            }

            if (Server != null && Server.Url != defaultUrl)
            {
                LogManager.Instance.Info($"Server URL was '{Server.Url}'");
                LogManager.Instance.Info($"Server URL was reset to default '{defaultUrl}'");
                Server.Url = defaultUrl;
            }
            else
            {
                LogManager.Instance.Info($"Server URL is already set to default '{defaultUrl}'");
            }

            if (GamePath != defaultPath)
            {
                LogManager.Instance.Info($"Game path was '{GamePath}'");
                LogManager.Instance.Info($"Game path was reset to default '{defaultPath}'");
                GamePath = defaultPath;
            }
            else
            {
                LogManager.Instance.Info($"Game path is already set to default '{defaultPath}'");
            }
            
            SaveSettings();
        }

        public string DefaultLocale { get; set; } = "English";

        private bool _isAddingServer;
        [JsonIgnore]
        public bool IsAddingServer
        {
            get => _isAddingServer;
            set => SetProperty(ref _isAddingServer, value);
        }

        private bool _allowSettings;
        [JsonIgnore]
        public bool AllowSettings
        {
            get => _allowSettings;
            set => SetProperty(ref _allowSettings, value);
        }

        private bool _gameRunning;
        [JsonIgnore]
        public bool GameRunning
        {
            get => _gameRunning;
            set => SetProperty(ref _gameRunning, value);
        }

        private LauncherAction _launcherStartGameAction;
        public LauncherAction LauncherStartGameAction
        {
            get => _launcherStartGameAction;
            set => SetProperty(ref _launcherStartGameAction, value);
        }

        private bool _useAutoLogin;
        public bool UseAutoLogin
        {
            get => _useAutoLogin;
            set => SetProperty(ref _useAutoLogin, value);
        }

        private bool _isDevMode;

        public bool IsDevMode
        {
            get => _isDevMode;
            set => SetProperty(ref _isDevMode, value);
        }

        private string _gamePath;
        public string GamePath
        {
            get => _gamePath;
            set => SetProperty(ref _gamePath, value);
        }

        private string[] _excludeFromCleanup = [];

        public string[] ExcludeFromCleanup
        {
            get => _excludeFromCleanup;
            set => SetProperty(ref _excludeFromCleanup, value);
        }

        public ServerSetting Server { get; set; } = new ServerSetting();

        public Settings()
        {
            if (!File.Exists(LauncherSettingsProvider.DefaultSettingsFileLocation))
            {
                LogManager.Instance.Warning("Launcher config not found");
                LogManager.Instance.Info($"Creating launcher config: {LauncherSettingsProvider.DefaultSettingsFileLocation}");
                LauncherStartGameAction = LauncherAction.MinimizeAction;
                UseAutoLogin = true;
                GamePath = Directory.GetParent(Environment.CurrentDirectory).FullName;
                IsDevMode = false;

                Server = new ServerSetting
                {
                    Name = "SPT", 
                    Url = "https://127.0.0.1:6969"
                };
                SaveSettings();
            }
            
            LogManager.Instance.Info($"Using launcher config at: {LauncherSettingsProvider.DefaultSettingsFileLocation}");
        }
    }
}
