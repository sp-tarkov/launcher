/* LocalizationProvider.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Controllers;
using SPT.Launcher.Extensions;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SPT.Launcher.Helpers
{
    public static class LocalizationProvider
    {
        public static string DefaultLocaleFolderPath = Path.Join(Environment.CurrentDirectory, "SPT_Data", "Launcher", "Locales");

        public static Dictionary<string, string> LocaleNameDictionary = GetLocaleDictionary("native_name");

        public static event EventHandler LocaleChanged = delegate { };

        public static void LoadLocalByName(string localeName)
        {
            string localeRomanName = LocaleNameDictionary.GetKeyByValue(localeName);

            if (String.IsNullOrEmpty(localeRomanName))
            {
                localeRomanName = localeName;
            }
            
            LoadLocaleFromFile(localeRomanName);
        }

        public static void LoadLocaleFromFile(string localeRomanName)
        {
            var localePath = Path.Join(DefaultLocaleFolderPath, $"{localeRomanName}.json");
            LocaleData newLocale = Json.LoadClassWithoutSaving<LocaleData>(localePath);

            if (newLocale != null)
            {
                foreach (var prop in Instance.GetType().GetProperties())
                {
                    prop.SetValue(Instance, newLocale.GetType().GetProperty(prop.Name).GetValue(newLocale));
                }

                LauncherSettingsProvider.Instance.DefaultLocale = localeRomanName;
                LauncherSettingsProvider.Instance.SaveSettings();

                LocaleChanged(null, EventArgs.Empty);

                return;
            }

            LogManager.Instance.Error($"Could not load locale: {localePath}");
        }

        public static void TryAutoSetLocale()
        {
            // get local dictionary based on ietf_tag property in locale files. like: ("English", "en")
            // "English" being the file name
            var localeTagDictionary = GetLocaleDictionary("ietf_tag");

            // get system locale. Like: "en-US"
            var tag = CultureInfo.CurrentUICulture.IetfLanguageTag;

            // get the locale file name from the dictionary based on the input tag. If it matches, or starts with the value
            var localeRomanName = localeTagDictionary.GetKeyByInput(tag);
            
            if (String.IsNullOrEmpty(localeRomanName))
            {
                localeRomanName = "English";
            }
            
            LoadLocaleFromFile(localeRomanName);
        }

        public static LocaleData GenerateEnglishLocale()
        {
            //Create default english locale data and save if the default locale data file dosen't exist.
            //This is to (hopefully) prevent the launcher from becoming 100% broken if no locale files exist or the locale files are outdated (missing data).
            LocaleData englishLocale = new LocaleData();

            #region Set All English Defaults
            englishLocale.ietf_tag = "en";
            englishLocale.native_name = "English";
            englishLocale.retry = "Retry";
            englishLocale.server_connecting = "Connecting";
            englishLocale.server_unavailable_format_1 = "No server available at: '{0}' to connect to\nEnsure you have run 'SPT.Server.exe' first.";
            englishLocale.no_servers_available = "No SPT Servers found. Ensure your SPT sever is running and check the server URL is correct in the settings page.";
            englishLocale.settings_menu = "Settings";
            englishLocale.back = "Back";
            englishLocale.wipe_profile = "Wipe Profile";
            englishLocale.username = "Username";
            englishLocale.update = "Update";
            englishLocale.edit_account_update_error = "An issue occurred while updating your profile.";
            englishLocale.register = "Register";
            englishLocale.go_to_register = "Go to Register";
            englishLocale.registration_failed = "Registration Failed.";
            englishLocale.registration_question_format_1 = "Profile '{0}' does not exist.\n\nWould you like to create it?";
            englishLocale.login_or_register = "Login / Register";
            englishLocale.go_to_login = "Go to Login";
            englishLocale.login_automatically = "Login Automatically";
            englishLocale.incorrect_login = "Username is incorrect";
            englishLocale.login_failed = "Login Failed";
            englishLocale.edition = "Edition";
            englishLocale.id = "ID";
            englishLocale.logout = "Logout";
            englishLocale.account = "Account";
            englishLocale.edit_account = "Edit Account";
            englishLocale.start_game = "Start Game";
            englishLocale.installed_in_live_game_warning = "SPT shouldn't be installed into the live game directory. Please install SPT into a copy of the game directory elsewhere on your computer.";
            englishLocale.no_official_game_warning = "Escape From Tarkov isn't installed on your computer. Ensure your BSG launcher can start EFT before starting SPT.";
            englishLocale.eft_exe_not_found_warning = "EscapeFromTarkov.exe not found at game path. Ensure the folder you installed SPT into has this file.";
            englishLocale.account_exist = "Account already exists";
            englishLocale.url = "URL";
            englishLocale.default_language = "Default Language";
            englishLocale.game_path = "Game Path";
            englishLocale.clear_game_settings = "Clear Game Settings";
            englishLocale.clear_game_settings_succeeded = "Game settings cleared.";
            englishLocale.clear_game_settings_failed = "An issue occurred while clearing game settings.";
            englishLocale.load_live_settings = "Load Live Settings";
            englishLocale.load_live_settings_succeeded = "Game settings copied from live";
            englishLocale.load_live_settings_failed = "Failed to copy live settings";
            englishLocale.remove_registry_keys = "Remove Registry Keys";
            englishLocale.remove_registry_keys_succeeded = "Registry keys removed.";
            englishLocale.remove_registry_keys_failed = "An issue occurred while removing registry keys.";
            englishLocale.clean_temp_files = "Clean Temp Files";
            englishLocale.clean_temp_files_succeeded = "Temp files cleaned";
            englishLocale.clean_temp_files_failed = "Some issues occurred while cleaning temp files";
            englishLocale.select_folder = "Select Folder";
            englishLocale.minimize_action = "Minimize";
            englishLocale.do_nothing_action = "Do nothing";
            englishLocale.exit_action = "Close Launcher";
            englishLocale.on_game_start = "On Game Start";
            englishLocale.game = "Game";
            englishLocale.wipe_warning = "Changing your account edition requires a profile wipe. This will reset your game prgrogess.";
            englishLocale.cancel = "Cancel";
            englishLocale.need_an_account = "Don't have an account yet?";
            englishLocale.have_an_account = "Already have an account?";
            englishLocale.reapply_patch = "Reapply Patch";
            englishLocale.failed_to_receive_patches = "Failed to receive patches";
            englishLocale.failed_core_patch = "Core patch failed";
            englishLocale.failed_mod_patch = "Mod patch failed";
            englishLocale.ok = "OK";
            englishLocale.account_page_denied = "Account page denied. Either you are not logged in or the game is running.";
            englishLocale.account_updated = "Your account has been updated";
            englishLocale.nickname = "Nickname";
            englishLocale.side = "Side";
            englishLocale.level = "Level";
            englishLocale.game_path = "Game Path";
            englishLocale.patching = "Patching";
            englishLocale.file_mismatch_dialog_message = "We noticed your EFT files do not match what we expected to see for SPT: {0}" +
                "\nPlease check you have the latest version of live EFT installed" +
                "\nIf not, delete SPT, update live EFT and run the Installer in an empty folder again" +
                "\n\nAre you sure you want to proceed?";
            englishLocale.yes = "Yes";
            englishLocale.no = "No";
            englishLocale.open_folder = "Open Folder";
            englishLocale.select_edition = "Select Edition";
            englishLocale.profile_created = "Profile Created";
            englishLocale.next_level_in = "Next level in";
            englishLocale.copied = "Copied";
            englishLocale.no_profile_data = "No profile data";
            englishLocale.profile_version_mismath = "Your profile was made using a different version of SPT and may have issues";
            englishLocale.profile_removed = "Profile removed";
            englishLocale.profile_removal_failed = "Failed to remove profile";
            englishLocale.profile_remove_question_format_1 = "Permanently remove profile '{0}'?";
            englishLocale.i_understand = "I Understand";
            englishLocale.game_version_mismatch_format_2 = "SPT is unable to run, this is because SPT expected to find EFT version '{1}',\nbut instead found version '{0}'\n\nEnsure you've downgraded your EFT as described in the install guide\non the page you downloaded SPT from";
            englishLocale.description = "Description";
            englishLocale.author = "Author";
            englishLocale.wipe_on_start = "Wipe profile on game start";
            englishLocale.copy_live_settings_question = "Would you like to copy your live game settings to spt";
            englishLocale.mod_not_in_server_warning = "This mod was found in your profile, but is not loaded on the server";
            englishLocale.active_server_mods = "Active Server Mods";
            englishLocale.active_server_mods_info_text = "These mods are currently running on the server";
            englishLocale.inactive_server_mods = "Inactive Server Mods";
            englishLocale.inactive_server_mods_info_text =
                "These mods have not been loaded by the server, but your profile has used them in the past";
            englishLocale.open_link_question_format_1 = "Are you sure you want to open the following link: \n{0}";
            englishLocale.open_link = "Open Link";
            englishLocale.dev_mode = "Developer Mode";
            englishLocale.failed_to_save_settings = "Failed to save settings";
            englishLocale.register_failed_name_limit = "name cannot exceed 15 characters";
            englishLocale.copy_failed = "Failed to copy data to clipboard";
            englishLocale.copy_logs_to_clipboard = "Copy logs to clipboard";
            #endregion

            Directory.CreateDirectory(LocalizationProvider.DefaultLocaleFolderPath);
            LauncherSettingsProvider.Instance.DefaultLocale = "English";
            LauncherSettingsProvider.Instance.SaveSettings();
            Json.SaveWithFormatting(Path.Join(LocalizationProvider.DefaultLocaleFolderPath, "English.json"), englishLocale, Newtonsoft.Json.Formatting.Indented);

            return englishLocale;
        }

        public static Dictionary<string, string> GetLocaleDictionary(string property)
        {
            List<FileInfo> localeFiles = new List<FileInfo>(Directory.GetFiles(DefaultLocaleFolderPath).Select(x => new FileInfo(x)).ToList());
            Dictionary<string, string> localeDictionary = new Dictionary<string, string>();

            foreach (FileInfo file in localeFiles)
            {
                localeDictionary.Add(file.Name.Replace(".json", ""), Json.GetPropertyByName<string>(file.FullName, property));
            }

            return localeDictionary;
        }
        public static ObservableCollection<string> GetAvailableLocales()
        {
            return new ObservableCollection<string>(LocaleNameDictionary.Values);
        }

        public static LocaleData Instance { get; private set; } = Json.LoadClassWithoutSaving<LocaleData>(Path.Join(DefaultLocaleFolderPath, $"{LauncherSettingsProvider.Instance.DefaultLocale}.json")) ?? GenerateEnglishLocale();
    }

    public class LocaleData : NotifyPropertyChangedBase
    {
        //this is going to be some pretty long boiler plate code. So I'm putting everything into regions.

        #region All Properties
        
        #region
        private string _copy_logs_to_clipboard;
        public string copy_logs_to_clipboard
        {
            get => _copy_logs_to_clipboard;
            set => SetProperty(ref _copy_logs_to_clipboard, value);
        }
        
        #endregion
        
        #region copy_failed
        private string _copy_failed;
        public string copy_failed
        {
            get => _copy_failed;
            set => SetProperty(ref _copy_failed, value);
        }
        #endregion
        
        #region register_failed_name_limit

        private string _register_failed_name_limit;

        public string register_failed_name_limit
        {
            get => _register_failed_name_limit;
            set => SetProperty(ref _register_failed_name_limit, value);
        }
        #endregion
        
        #region failed_to_save_settings

        private string _failed_to_save_settings;

        public string failed_to_save_settings
        {
            get => _failed_to_save_settings;
            set => SetProperty(ref _failed_to_save_settings, value);
        }
        #endregion
        
        #region dev_mode
        private string _dev_mode;
        public string dev_mode
        {
            get => _dev_mode;
            set => SetProperty(ref _dev_mode, value);
        }
        #endregion

        #region ietf_tag

        private string _ietf_tag;

        public string ietf_tag
        {
            get => _ietf_tag;
            set => SetProperty(ref _ietf_tag, value);
        }
        #endregion
        
        #region native_name
        private string _native_name;
        public string native_name
        {
            get => _native_name;
            set => SetProperty(ref _native_name, value);
        }
        #endregion

        #region retry
        private string _retry;
        public string retry
        {
            get => _retry;
            set => SetProperty(ref _retry, value);
        }
        #endregion

        #region server_connecting
        private string _server_connecting;
        public string server_connecting
        {
            get => _server_connecting;
            set => SetProperty(ref _server_connecting, value);
        }
        #endregion

        #region server_unavailable_format_1
        private string _server_unavailable_format_1;
        public string server_unavailable_format_1
        {
            get => _server_unavailable_format_1;
            set => SetProperty(ref _server_unavailable_format_1, value);
        }
        #endregion

        #region no_servers_available
        private string _no_servers_available;
        public string no_servers_available
        {
            get => _no_servers_available;
            set => SetProperty(ref _no_servers_available, value);
        }
        #endregion

        #region settings_menu
        private string _settings_menu;
        public string settings_menu
        {
            get => _settings_menu;
            set => SetProperty(ref _settings_menu, value);
        }
        #endregion

        #region back
        private string _back;
        public string back
        {
            get => _back;
            set => SetProperty(ref _back, value);
        }
        #endregion

        #region wipe_profile
        private string _wipe_profile;
        public string wipe_profile
        {
            get => _wipe_profile;
            set => SetProperty(ref _wipe_profile, value);
        }
        #endregion

        #region username
        private string _username;
        public string username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }
        #endregion

        #region update
        private string _update;
        public string update
        {
            get => _update;
            set => SetProperty(ref _update, value);
        }
        #endregion

        #region edit_account_update_error
        private string _edit_account_update_error;
        public string edit_account_update_error
        {
            get => _edit_account_update_error;
            set => SetProperty(ref _edit_account_update_error, value);
        }
        #endregion

        #region register
        private string _register;
        public string register
        {
            get => _register;
            set => SetProperty(ref _register, value);
        }
        #endregion

        #region go_to_register
        private string _go_to_register;
        public string go_to_register
        {
            get => _go_to_register;
            set => SetProperty(ref _go_to_register, value);
        }
        #endregion

        #region login_or_register
        private string _login_or_register;
        public string login_or_register
        {
            get => _login_or_register;
            set => SetProperty(ref _login_or_register, value);
        }
        #endregion

        #region go_to_login
        private string _go_to_login;
        public string go_to_login
        {
            get => _go_to_login;
            set => SetProperty(ref _go_to_login, value);
        }
        #endregion

        #region login_automatically
        private string _login_automatically;
        public string login_automatically
        {
            get => _login_automatically;
            set => SetProperty(ref _login_automatically, value);
        }
        #endregion

        #region incorrect_login
        private string _incorrect_login;
        public string incorrect_login
        {
            get => _incorrect_login;
            set => SetProperty(ref _incorrect_login, value);
        }
        #endregion

        #region login_failed
        private string _login_failed;
        public string login_failed
        {
            get => _login_failed;
            set => SetProperty(ref _login_failed, value);
        }
        #endregion

        #region edition
        private string _edition;
        public string edition
        {
            get => _edition;
            set => SetProperty(ref _edition, value);
        }
        #endregion

        #region id
        private string _id;
        public string id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        #endregion

        #region logout
        private string _logout;
        public string logout
        {
            get => _logout;
            set => SetProperty(ref _logout, value);
        }
        #endregion

        #region account
        private string _account;
        public string account
        {
            get => _account;
            set => SetProperty(ref _account, value);
        }
        #endregion

        #region edit_account
        private string _edit_account;
        public string edit_account
        {
            get => _edit_account;
            set => SetProperty(ref _edit_account, value);
        }
        #endregion

        #region start_game
        private string _start_game;
        public string start_game
        {
            get => _start_game;
            set => SetProperty(ref _start_game, value);
        }
        #endregion

        #region installed_in_live_game_warning
        private string _installed_in_live_game_warning;
        public string installed_in_live_game_warning
        {
            get => _installed_in_live_game_warning;
            set => SetProperty(ref _installed_in_live_game_warning, value);
        }
        #endregion

        #region no_official_game_warning
        private string _no_official_game_warning;
        public string no_official_game_warning
        {
            get => _no_official_game_warning;
            set => SetProperty(ref _no_official_game_warning, value);
        }
        #endregion

        #region eft_exe_not_found_warning
        private string _eft_exe_not_found_warning;
        public string eft_exe_not_found_warning
        {
            get => _eft_exe_not_found_warning;
            set => SetProperty(ref _eft_exe_not_found_warning, value);
        }
        #endregion

        #region account_exist
        private string _account_exist;
        public string account_exist
        {
            get => _account_exist;
            set => SetProperty(ref _account_exist, value);
        }
        #endregion

        #region url
        private string _url;
        public string url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
        #endregion

        #region default_language
        private string _default_language;
        public string default_language
        {
            get => _default_language;
            set => SetProperty(ref _default_language, value);
        }
        #endregion

        #region game_path
        private string _game_path;
        public string game_path
        {
            get => _game_path;
            set => SetProperty(ref _game_path, value);
        }
        #endregion

        #region clear_game_settings
        private string _clear_game_settings;
        public string clear_game_settings
        {
            get => _clear_game_settings;
            set => SetProperty(ref _clear_game_settings, value);
        }
        #endregion

        #region clear_game_settings_succeeded
        private string _clear_game_settings_succeeded;
        public string clear_game_settings_succeeded
        {
            get => _clear_game_settings_succeeded;
            set => SetProperty(ref _clear_game_settings_succeeded, value);
        }
        #endregion

        #region clear_game_settings_failed
        private string _clear_game_settings_failed;
        public string clear_game_settings_failed
        {
            get => _clear_game_settings_failed;
            set => SetProperty(ref _clear_game_settings_failed, value);
        }
        #endregion

        #region load_live_settings
        private string _load_live_settings;
        public string load_live_settings
        {
            get => _load_live_settings;
            set => SetProperty(ref _load_live_settings, value);
        }
        #endregion

        #region load_live_settings_failed
        private string _load_live_settings_failed;
        public string load_live_settings_failed
        {
            get => _load_live_settings_failed;
            set => SetProperty(ref _load_live_settings_failed, value);
        }
        #endregion

        #region load_live_settings_succeeded
        private string _load_live_settings_succeeded;
        public string load_live_settings_succeeded
        {
            get => _load_live_settings_succeeded;
            set => SetProperty(ref _load_live_settings_succeeded, value);
        }
        #endregion

        #region remove_registry_keys
        private string _remove_registry_keys;
        public string remove_registry_keys
        {
            get => _remove_registry_keys;
            set => SetProperty(ref _remove_registry_keys, value);
        }
        #endregion

        #region remove_registry_keys_succeeded
        private string _remove_registry_keys_succeeded;
        public string remove_registry_keys_succeeded
        {
            get => _remove_registry_keys_succeeded;
            set => SetProperty(ref _remove_registry_keys_succeeded, value);
        }
        #endregion

        #region remove_registry_keys_failed
        private string _remove_registry_keys_failed;
        public string remove_registry_keys_failed
        {
            get => _remove_registry_keys_failed;
            set => SetProperty(ref _remove_registry_keys_failed, value);
        }
        #endregion

        #region clean_temp_files
        private string _clean_temp_files;
        public string clean_temp_files
        {
            get => _clean_temp_files;
            set => SetProperty(ref _clean_temp_files, value);
        }
        #endregion

        #region clean_temp_files_succeeded
        private string _clean_temp_files_succeeded;
        public string clean_temp_files_succeeded
        {
            get => _clean_temp_files_succeeded;
            set => SetProperty(ref _clean_temp_files_succeeded, value);
        }
        #endregion

        #region clean_temp_files_failed
        private string _clean_temp_files_failed;
        public string clean_temp_files_failed
        {
            get => _clean_temp_files_failed;
            set => SetProperty(ref _clean_temp_files_failed, value);
        }
        #endregion

        #region select_folder
        private string _select_folder;
        public string select_folder
        {
            get => _select_folder;
            set => SetProperty(ref _select_folder, value);
        }
        #endregion

        #region registration_failed
        private string _registration_failed;
        public string registration_failed
        {
            get => _registration_failed;
            set => SetProperty(ref _registration_failed, value);
        }
        #endregion

        #region registration_question_format_1
        private string _registration_question_format_1;
        public string registration_question_format_1
        {
            get => _registration_question_format_1;
            set => SetProperty(ref _registration_question_format_1, value);
        }
        #endregion

        #region minimize_action
        private string _minimize_action;
        public string minimize_action
        {
            get => _minimize_action;
            set => SetProperty(ref _minimize_action, value);
        }
        #endregion

        #region do_nothing_action
        private string _do_nothing_action;
        public string do_nothing_action
        {
            get => _do_nothing_action;
            set => SetProperty(ref _do_nothing_action, value);
        }
        #endregion

        #region exit_action
        private string _exit_action;
        public string exit_action
        {
            get => _exit_action;
            set => SetProperty(ref _exit_action, value);
        }
        #endregion

        #region on_game_start
        private string _on_game_start;
        public string on_game_start
        {
            get => _on_game_start;
            set => SetProperty(ref _on_game_start, value);
        }
        #endregion

        #region game
        private string _game;
        public string game
        {
            get => _game;
            set => SetProperty(ref _game, value);
        }
        #endregion

        #region wipe_warning
        private string _wipe_warning;
        public string wipe_warning
        {
            get => _wipe_warning;
            set => SetProperty(ref _wipe_warning, value);
        }
        #endregion

        #region cancel
        private string _cancel;
        public string cancel
        {
            get => _cancel;
            set => SetProperty(ref _cancel, value);
        }
        #endregion

        #region need_an_account
        private string _need_an_account;
        public string need_an_account
        {
            get => _need_an_account;
            set => SetProperty(ref _need_an_account, value);
        }
        #endregion

        #region have_an_account
        private string _have_an_account;
        public string have_an_account
        {
            get => _have_an_account;
            set => SetProperty(ref _have_an_account, value);
        }
        #endregion

        #region reapply_patch
        private string _reapply_patch;
        public string reapply_patch
        {
            get => _reapply_patch;
            set => SetProperty(ref _reapply_patch, value);
        }
        #endregion

        #region failed_to_receive_patches
        private string _failed_to_receive_patches;
        public string failed_to_receive_patches
        {
            get => _failed_to_receive_patches;
            set => SetProperty(ref _failed_to_receive_patches, value);
        }
        #endregion

        #region failed_core_patch
        private string _failed_core_patch;
        public string failed_core_patch
        {
            get => _failed_core_patch;
            set => SetProperty(ref _failed_core_patch, value);
        }
        #endregion

        #region failed_mod_patch
        private string _failed_mod_patch;
        public string failed_mod_patch
        {
            get => _failed_mod_patch;
            set => SetProperty(ref _failed_mod_patch, value);
        }
        #endregion

        #region OK
        private string _ok;
        public string ok
        {
            get => _ok;
            set => SetProperty(ref _ok, value);
        }
        #endregion

        #region account_page_denied
        private string _account_page_denied;
        public string account_page_denied
        {
            get => _account_page_denied;
            set => SetProperty(ref _account_page_denied, value);
        }
        #endregion

        #region account_updated
        private string _account_updated;
        public string account_updated
        {
            get => _account_updated;
            set => SetProperty(ref _account_updated, value);
        }
        #endregion

        #region nickname
        private string _nickname;
        public string nickname
        {
            get => _nickname;
            set => SetProperty(ref _nickname, value);
        }
        #endregion

        #region side
        private string _side;
        public string side
        {
            get => _side;
            set => SetProperty(ref _side, value);
        }
        #endregion

        #region level
        private string _level;
        public string level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        #endregion

        #region patching
        private string _patching;
        public string patching
        {
            get => _patching;
            set => SetProperty(ref _patching, value);
        }
        #endregion

        #region file_mismatch_dialog_message
        private string _file_mismatch_dialog_message;
        public string file_mismatch_dialog_message
        {
            get => _file_mismatch_dialog_message;
            set => SetProperty(ref _file_mismatch_dialog_message, value);
        }
        #endregion

        #region yes
        private string _yes;
        public string yes
        {
            get => _yes;
            set => SetProperty(ref _yes, value);
        }
        #endregion

        #region no
        private string _no;
        public string no
        {
            get => _no;
            set => SetProperty(ref _no, value);
        }
        #endregion

        #region profile_created
        private string _profile_created;
        public string profile_created
        {
            get => _profile_created;
            set => SetProperty(ref _profile_created, value);
        }
        #endregion

        #region open_folder
        private string _open_folder;
        public string open_folder
        {
            get => _open_folder;
            set => SetProperty(ref _open_folder, value);
        }
        #endregion

        #region select_edition
        private string _select_edition;
        public string select_edition
        {
            get => _select_edition;
            set => SetProperty(ref _select_edition, value);
        }
        #endregion

        #region copied
        private string _copied;
        public string copied
        {
            get => _copied;
            set => SetProperty(ref _copied, value);
        }
        #endregion

        #region next_level_in
        private string _next_level_in;
        public string next_level_in
        {
            get => _next_level_in;
            set => SetProperty(ref _next_level_in, value);
        }
        #endregion

        #region no_profile_data
        private string _no_profile_data;
        public string no_profile_data
        {
            get => _no_profile_data;
            set => SetProperty(ref _no_profile_data, value);
        }
        #endregion

        #region profile_version_mismatch
        private string _profile_version_mismath;
        public string profile_version_mismath
        {
            get => _profile_version_mismath;
            set => SetProperty(ref _profile_version_mismath, value);
        }
        #endregion

        #region profile_removed
        private string _profile_removed;
        public string profile_removed
        {
            get => _profile_removed;
            set => SetProperty(ref _profile_removed, value);
        }
        #endregion

        #region profile_removal_failed
        private string _profile_removal_failed;
        public string profile_removal_failed
        {
            get => _profile_removal_failed;
            set => SetProperty(ref _profile_removal_failed, value);
        }
        #endregion

        #region profile_remove_question_format_1
        private string _profile_remove_question_format_1;
        public string profile_remove_question_format_1
        {
            get => _profile_remove_question_format_1;
            set => SetProperty(ref _profile_remove_question_format_1, value);
        }
        #endregion

        #region i_understand
        private string _i_understand;
        public string i_understand
        {
            get => _i_understand;
            set => SetProperty(ref _i_understand, value);
        }
        #endregion

        #region game_version_mismatch_format_2
        private string _game_version_mismatch_format_2;
        public string game_version_mismatch_format_2
        {
            get => _game_version_mismatch_format_2;
            set => SetProperty(ref _game_version_mismatch_format_2, value);
        }
        #endregion

        #region description
        private string _description;
        public string description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        #endregion

        #region author
        private string _author;
        public string author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        #endregion

        #region wipe_on_start
        private string _wipe_on_start;
        public string wipe_on_start
        {
            get => _wipe_on_start;
            set => SetProperty(ref _wipe_on_start, value);
        }
        #endregion

        #region copy_live_settings_question
        private string _copy_live_settings_question;
        public string copy_live_settings_question
        {
            get => _copy_live_settings_question;
            set => SetProperty(ref _copy_live_settings_question, value);
        }
        #endregion
        
        #region mod_not_in_server_warning
        private string _mod_not_in_server_warning;
        public string mod_not_in_server_warning
        {
            get => _mod_not_in_server_warning;
            set => SetProperty(ref _mod_not_in_server_warning, value);
        }
        #endregion
        
        #region active_server_mods
        private string _active_server_mods;
        public string active_server_mods
        {
            get => _active_server_mods;
            set => SetProperty(ref _active_server_mods, value);
        }
        #endregion
        
        #region active_server_mods_info_text

        private string _active_server_mods_info_text;

        public string active_server_mods_info_text
        {
            get => _active_server_mods_info_text;
            set => SetProperty(ref _active_server_mods_info_text, value);
        }
        #endregion
        
        #region inactive_server_mods
        private string _inactive_server_mods;
        public string inactive_server_mods
        {
            get => _inactive_server_mods;
            set => SetProperty(ref _inactive_server_mods, value);
        }
        #endregion
        
        #region inactive_server_mods_info_text

        private string _inactive_server_mods_info_text;

        public string inactive_server_mods_info_text
        {
            get => _inactive_server_mods_info_text;
            set => SetProperty(ref _inactive_server_mods_info_text, value);
        }
        #endregion
        
        #region open_link_question_format_1

        private string _open_link_question_format_1;

        public string open_link_question_format_1
        {
            get => _open_link_question_format_1;
            set => SetProperty(ref _open_link_question_format_1, value);
        }
        #endregion
        
        #region open_link

        private string _open_link;

        public string open_link
        {
            get => _open_link;
            set => SetProperty(ref _open_link, value);
        }
        #endregion

        #region core_dll_file_version_mismatch
        private string _core_dll_file_version_mismatch;

        public string core_dll_file_version_mismatch
        {
            get => _core_dll_file_version_mismatch;
            set => SetProperty(ref _core_dll_file_version_mismatch, value);
        }

        #endregion

        #endregion
    }
}
