/* LocalizationProvider.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Extensions;
using SPT.Launcher.MiniCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using SPT.Launcher.Controllers;

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
            englishLocale.password = "Password";
            englishLocale.update = "Update";
            englishLocale.edit_account_update_error = "An issue occurred while updating your profile.";
            englishLocale.register = "Register";
            englishLocale.go_to_register = "Go to Register";
            englishLocale.registration_failed = "Registration Failed.";
            englishLocale.registration_question_format_1 = "Profile '{0}' does not exist.\n\nWould you like to create it?";
            englishLocale.login_or_register = "Login / Register";
            englishLocale.go_to_login = "Go to Login";
            englishLocale.login_automatically = "Login Automatically";
            englishLocale.incorrect_login = "Username or password is incorrect";
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
            englishLocale.new_password = "New Password";
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

    public class LocaleData : INotifyPropertyChanged
    {
        //this is going to be some pretty long boiler plate code. So I'm putting everything into regions.

        #region All Properties
        
        #region register_failed_name_limit

        private string _register_failed_name_limit;

        public string register_failed_name_limit
        {
            get => _register_failed_name_limit;
            set
            {
                if (_register_failed_name_limit != value)
                {
                    _register_failed_name_limit = value;
                    RaisePropertyChanged(nameof(register_failed_name_limit));
                }
            }
        }
        #endregion
        
        #region failed_to_save_settings

        private string _failed_to_save_settings;

        public string failed_to_save_settings
        {
            get => _failed_to_save_settings;
            set
            {
                if (_failed_to_save_settings != value)
                {
                    _failed_to_save_settings = value;
                    RaisePropertyChanged(nameof(failed_to_save_settings));
                }
            }
        }
        #endregion
        
        #region dev_mode
        private string _dev_mode;
        public string dev_mode
        {
            get => _dev_mode;
            set
            {
                if (_dev_mode != value)
                {
                    _dev_mode = value;
                    RaisePropertyChanged(nameof(dev_mode));
                }
            }
        }
        #endregion

        #region ietf_tag

        private string _ietf_tag;

        public string ietf_tag
        {
            get => _ietf_tag;
            set
            {
                if (_ietf_tag != value)
                {
                    _ietf_tag = value;
                    RaisePropertyChanged(nameof(ietf_tag));
                }
            }
        }
        #endregion
        
        #region native_name
        private string _native_name;
        public string native_name
        {
            get => _native_name;
            set
            {
                if (_native_name != value)
                {
                    _native_name = value;
                    RaisePropertyChanged(nameof(native_name));
                }
            }
        }
        #endregion

        #region retry
        private string _retry;
        public string retry
        {
            get => _retry;
            set
            {
                if (_retry != value)
                {
                    _retry = value;
                    RaisePropertyChanged(nameof(retry));
                }
            }
        }
        #endregion

        #region server_connecting
        private string _server_connecting;
        public string server_connecting
        {
            get => _server_connecting;
            set
            {
                if (_server_connecting != value)
                {
                    _server_connecting = value;
                    RaisePropertyChanged(nameof(server_connecting));
                }
            }
        }
        #endregion

        #region server_unavailable_format_1
        private string _server_unavailable_format_1;
        public string server_unavailable_format_1
        {
            get => _server_unavailable_format_1;
            set
            {
                if (_server_unavailable_format_1 != value)
                {
                    _server_unavailable_format_1 = value;
                    RaisePropertyChanged(nameof(server_unavailable_format_1));
                }
            }
        }
        #endregion

        #region no_servers_available
        private string _no_servers_available;
        public string no_servers_available
        {
            get => _no_servers_available;
            set
            {
                if (_no_servers_available != value)
                {
                    _no_servers_available = value;
                    RaisePropertyChanged(nameof(no_servers_available));
                }
            }
        }
        #endregion

        #region settings_menu
        private string _settings_menu;
        public string settings_menu
        {
            get => _settings_menu;
            set
            {
                if (_settings_menu != value)
                {
                    _settings_menu = value;
                    RaisePropertyChanged(nameof(settings_menu));
                }
            }
        }
        #endregion

        #region back
        private string _back;
        public string back
        {
            get => _back;
            set
            {
                if (_back != value)
                {
                    _back = value;
                    RaisePropertyChanged(nameof(back));
                }
            }
        }
        #endregion

        #region wipe_profile
        private string _wipe_profile;
        public string wipe_profile
        {
            get => _wipe_profile;
            set
            {
                if (_wipe_profile != value)
                {
                    _wipe_profile = value;
                    RaisePropertyChanged(nameof(wipe_profile));
                }
            }
        }
        #endregion

        #region username
        private string _username;
        public string username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    RaisePropertyChanged(nameof(username));
                }
            }
        }
        #endregion

        #region password
        private string _password;
        public string password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    RaisePropertyChanged(nameof(password));
                }
            }
        }
        #endregion

        #region update
        private string _update;
        public string update
        {
            get => _update;
            set
            {
                if (_update != value)
                {
                    _update = value;
                    RaisePropertyChanged(nameof(update));
                }
            }
        }
        #endregion

        #region edit_account_update_error
        private string _edit_account_update_error;
        public string edit_account_update_error
        {
            get => _edit_account_update_error;
            set
            {
                if (_edit_account_update_error != value)
                {
                    _edit_account_update_error = value;
                    RaisePropertyChanged(nameof(edit_account_update_error));
                }
            }
        }
        #endregion

        #region register
        private string _register;
        public string register
        {
            get => _register;
            set
            {
                if (_register != value)
                {
                    _register = value;
                    RaisePropertyChanged(nameof(register));
                }
            }
        }
        #endregion

        #region go_to_register
        private string _go_to_register;
        public string go_to_register
        {
            get => _go_to_register;
            set
            {
                if (_go_to_register != value)
                {
                    _go_to_register = value;
                    RaisePropertyChanged(nameof(_go_to_register));
                }
            }
        }
        #endregion

        #region login_or_register
        private string _login_or_register;
        public string login_or_register
        {
            get => _login_or_register;
            set
            {
                if (_login_or_register != value)
                {
                    _login_or_register = value;
                    RaisePropertyChanged(nameof(login_or_register));
                }
            }
        }
        #endregion

        #region go_to_login
        private string _go_to_login;
        public string go_to_login
        {
            get => _go_to_login;
            set
            {
                if (_go_to_login != value)
                {
                    _go_to_login = value;
                    RaisePropertyChanged(nameof(go_to_login));
                }
            }
        }
        #endregion

        #region login_automatically
        private string _login_automatically;
        public string login_automatically
        {
            get => _login_automatically;
            set
            {
                if (_login_automatically != value)
                {
                    _login_automatically = value;
                    RaisePropertyChanged(nameof(login_automatically));
                }
            }
        }
        #endregion

        #region incorrect_login
        private string _incorrect_login;
        public string incorrect_login
        {
            get => _incorrect_login;
            set
            {
                if (_incorrect_login != value)
                {
                    _incorrect_login = value;
                    RaisePropertyChanged(nameof(incorrect_login));
                }
            }
        }
        #endregion

        #region login_failed
        private string _login_failed;
        public string login_failed
        {
            get => _login_failed;
            set
            {
                if (_login_failed != value)
                {
                    _login_failed = value;
                    RaisePropertyChanged(nameof(login_failed));
                }
            }
        }
        #endregion

        #region edition
        private string _edition;
        public string edition
        {
            get => _edition;
            set
            {
                if (_edition != value)
                {
                    _edition = value;
                    RaisePropertyChanged(nameof(edition));
                }
            }
        }
        #endregion

        #region id
        private string _id;
        public string id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChanged(nameof(id));
                }
            }
        }
        #endregion

        #region logout
        private string _logout;
        public string logout
        {
            get => _logout;
            set
            {
                if (_logout != value)
                {
                    _logout = value;
                    RaisePropertyChanged(nameof(logout));
                }
            }
        }
        #endregion

        #region account
        private string _account;
        public string account
        {
            get => _account;
            set
            {
                if (_account != value)
                {
                    _account = value;
                    RaisePropertyChanged(nameof(account));
                }
            }
        }
        #endregion

        #region edit_account
        private string _edit_account;
        public string edit_account
        {
            get => _edit_account;
            set
            {
                if (_edit_account != value)
                {
                    _edit_account = value;
                    RaisePropertyChanged(nameof(edit_account));
                }
            }
        }
        #endregion

        #region start_game
        private string _start_game;
        public string start_game
        {
            get => _start_game;
            set
            {
                if (_start_game != value)
                {
                    _start_game = value;
                    RaisePropertyChanged(nameof(start_game));
                }
            }
        }
        #endregion

        #region installed_in_live_game_warning
        private string _installed_in_live_game_warning;
        public string installed_in_live_game_warning
        {
            get => _installed_in_live_game_warning;
            set
            {
                if (_installed_in_live_game_warning != value)
                {
                    _installed_in_live_game_warning = value;
                    RaisePropertyChanged(nameof(installed_in_live_game_warning));
                }
            }
        }
        #endregion

        #region no_official_game_warning
        private string _no_official_game_warning;
        public string no_official_game_warning
        {
            get => _no_official_game_warning;
            set
            {
                if (_no_official_game_warning != value)
                {
                    _no_official_game_warning = value;
                    RaisePropertyChanged(nameof(no_official_game_warning));
                }
            }
        }
        #endregion

        #region eft_exe_not_found_warning
        private string _eft_exe_not_found_warning;
        public string eft_exe_not_found_warning
        {
            get => _eft_exe_not_found_warning;
            set
            {
                if (_eft_exe_not_found_warning != value)
                {
                    _eft_exe_not_found_warning = value;
                    RaisePropertyChanged(nameof(eft_exe_not_found_warning));
                }
            }
        }
        #endregion

        #region account_exist
        private string _account_exist;
        public string account_exist
        {
            get => _account_exist;
            set
            {
                if (_account_exist != value)
                {
                    _account_exist = value;
                    RaisePropertyChanged(nameof(account_exist));
                }
            }
        }
        #endregion

        #region url
        private string _url;
        public string url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    RaisePropertyChanged(nameof(url));
                }
            }
        }
        #endregion

        #region default_language
        private string _default_language;
        public string default_language
        {
            get => _default_language;
            set
            {
                if (_default_language != value)
                {
                    _default_language = value;
                    RaisePropertyChanged(nameof(default_language));
                }
            }
        }
        #endregion

        #region game_path
        private string _game_path;
        public string game_path
        {
            get => _game_path;
            set
            {
                if (_game_path != value)
                {
                    _game_path = value;
                    RaisePropertyChanged(nameof(game_path));
                }
            }
        }
        #endregion

        #region clear_game_settings
        private string _clear_game_settings;
        public string clear_game_settings
        {
            get => _clear_game_settings;
            set
            {
                if (_clear_game_settings != value)
                {
                    _clear_game_settings = value;
                    RaisePropertyChanged(nameof(clear_game_settings));
                }
            }
        }
        #endregion

        #region clear_game_settings_succeeded
        private string _clear_game_settings_succeeded;
        public string clear_game_settings_succeeded
        {
            get => _clear_game_settings_succeeded;
            set
            {
                if (_clear_game_settings_succeeded != value)
                {
                    _clear_game_settings_succeeded = value;
                    RaisePropertyChanged(nameof(clear_game_settings_succeeded));
                }
            }
        }
        #endregion

        #region clear_game_settings_failed
        private string _clear_game_settings_failed;
        public string clear_game_settings_failed
        {
            get => _clear_game_settings_failed;
            set
            {
                if (_clear_game_settings_failed != value)
                {
                    _clear_game_settings_failed = value;
                    RaisePropertyChanged(nameof(clear_game_settings_failed));
                }
            }
        }
        #endregion

        #region load_live_settings
        private string _load_live_settings;
        public string load_live_settings
        {
            get => _load_live_settings;
            set
            {
                if (_load_live_settings != value)
                {
                    _load_live_settings = value;
                    RaisePropertyChanged(nameof(load_live_settings));
                }
            }
        }
        #endregion

        #region load_live_settings_failed
        private string _load_live_settings_failed;
        public string load_live_settings_failed
        {
            get => _load_live_settings_failed;
            set
            {
                if (_load_live_settings_failed != value)
                {
                    _load_live_settings_failed = value;
                    RaisePropertyChanged(nameof(load_live_settings_failed));
                }
            }
        }
        #endregion

        #region load_live_settings_succeeded
        private string _load_live_settings_succeeded;
        public string load_live_settings_succeeded
        {
            get => _load_live_settings_succeeded;
            set
            {
                if (_load_live_settings_succeeded != value)
                {
                    _load_live_settings_succeeded = value;
                    RaisePropertyChanged(nameof(load_live_settings_succeeded));
                }
            }
        }
        #endregion

        #region remove_registry_keys
        private string _remove_registry_keys;
        public string remove_registry_keys
        {
            get => _remove_registry_keys;
            set
            {
                if (_remove_registry_keys != value)
                {
                    _remove_registry_keys = value;
                    RaisePropertyChanged(nameof(remove_registry_keys));
                }
            }
        }
        #endregion

        #region remove_registry_keys_succeeded
        private string _remove_registry_keys_succeeded;
        public string remove_registry_keys_succeeded
        {
            get => _remove_registry_keys_succeeded;
            set
            {
                if (_remove_registry_keys_succeeded != value)
                {
                    _remove_registry_keys_succeeded = value;
                    RaisePropertyChanged(nameof(remove_registry_keys_succeeded));
                }
            }
        }
        #endregion

        #region remove_registry_keys_failed
        private string _remove_registry_keys_failed;
        public string remove_registry_keys_failed
        {
            get => _remove_registry_keys_failed;
            set
            {
                if (_remove_registry_keys_failed != value)
                {
                    _remove_registry_keys_failed = value;
                    RaisePropertyChanged(nameof(remove_registry_keys_failed));
                }
            }
        }
        #endregion

        #region clean_temp_files
        private string _clean_temp_files;
        public string clean_temp_files
        {
            get => _clean_temp_files;
            set
            {
                if (_clean_temp_files != value)
                {
                    _clean_temp_files = value;
                    RaisePropertyChanged(nameof(clean_temp_files));
                }
            }
        }
        #endregion

        #region clean_temp_files_succeeded
        private string _clean_temp_files_succeeded;
        public string clean_temp_files_succeeded
        {
            get => _clean_temp_files_succeeded;
            set
            {
                if (_clean_temp_files_succeeded != value)
                {
                    _clean_temp_files_succeeded = value;
                    RaisePropertyChanged(nameof(clean_temp_files_succeeded));
                }
            }
        }
        #endregion

        #region clean_temp_files_failed
        private string _clean_temp_files_failed;
        public string clean_temp_files_failed
        {
            get => _clean_temp_files_failed;
            set
            {
                if (_clean_temp_files_failed != value)
                {
                    _clean_temp_files_failed = value;
                    RaisePropertyChanged(nameof(clean_temp_files_failed));
                }
            }
        }
        #endregion

        #region select_folder
        private string _select_folder;
        public string select_folder
        {
            get => _select_folder;
            set
            {
                if (_select_folder != value)
                {
                    _select_folder = value;
                    RaisePropertyChanged(nameof(select_folder));
                }
            }
        }
        #endregion

        #region registration_failed
        private string _registration_failed;
        public string registration_failed
        {
            get => _registration_failed;
            set
            {
                if (_registration_failed != value)
                {
                    _registration_failed = value;
                    RaisePropertyChanged(nameof(registration_failed));
                }
            }
        }
        #endregion

        #region registration_question_format_1
        private string _registration_question_format_1;
        public string registration_question_format_1
        {
            get => _registration_question_format_1;
            set
            {
                if(_registration_question_format_1 != value)
                {
                    _registration_question_format_1 = value;
                    RaisePropertyChanged(nameof(registration_question_format_1));
                }
            }
        }
        #endregion

        #region minimize_action
        private string _minimize_action;
        public string minimize_action
        {
            get => _minimize_action;
            set
            {
                if (_minimize_action != value)
                {
                    _minimize_action = value;
                    RaisePropertyChanged(nameof(minimize_action));
                }
            }
        }
        #endregion

        #region do_nothing_action
        private string _do_nothing_action;
        public string do_nothing_action
        {
            get => _do_nothing_action;
            set
            {
                if (_do_nothing_action != value)
                {
                    _do_nothing_action = value;
                    RaisePropertyChanged(nameof(do_nothing_action));
                }
            }
        }
        #endregion

        #region exit_action
        private string _exit_action;
        public string exit_action
        {
            get => _exit_action;
            set
            {
                if (_exit_action != value)
                {
                    _exit_action = value;
                    RaisePropertyChanged(nameof(exit_action));
                }
            }
        }
        #endregion

        #region on_game_start
        private string _on_game_start;
        public string on_game_start
        {
            get => _on_game_start;
            set
            {
                if (_on_game_start != value)
                {
                    _on_game_start = value;
                    RaisePropertyChanged(nameof(on_game_start));
                }
            }
        }
        #endregion

        #region game
        private string _game;
        public string game
        {
            get => _game;
            set
            {
                if (_game != value)
                {
                    _game = value;
                    RaisePropertyChanged(nameof(game));
                }
            }
        }
        #endregion

        #region new_password
        private string _new_password;
        public string new_password
        {
            get => _new_password;
            set
            {
                if (_new_password != value)
                {
                    _new_password = value;
                    RaisePropertyChanged(nameof(new_password));
                }
            }
        }
        #endregion

        #region wipe_warning
        private string _wipe_warning;
        public string wipe_warning
        {
            get => _wipe_warning;
            set
            {
                if (_wipe_warning != value)
                {
                    _wipe_warning = value;
                    RaisePropertyChanged(nameof(wipe_warning));
                }
            }
        }
        #endregion

        #region cancel
        private string _cancel;
        public string cancel
        {
            get => _cancel;
            set
            {
                if (_cancel != value)
                {
                    _cancel = value;
                    RaisePropertyChanged(nameof(cancel));
                }
            }
        }
        #endregion

        #region need_an_account
        private string _need_an_account;
        public string need_an_account
        {
            get => _need_an_account;
            set
            {
                if (_need_an_account != value)
                {
                    _need_an_account = value;
                    RaisePropertyChanged(nameof(need_an_account));
                }
            }
        }
        #endregion

        #region have_an_account
        private string _have_an_account;
        public string have_an_account
        {
            get => _have_an_account;
            set
            {
                if (_have_an_account != value)
                {
                    _have_an_account = value;
                    RaisePropertyChanged(nameof(have_an_account));
                }
            }
        }
        #endregion

        #region reapply_patch
        private string _reapply_patch;
        public string reapply_patch
        {
            get => _reapply_patch;
            set
            {
                if (_reapply_patch != value)
                {
                    _reapply_patch = value;
                    RaisePropertyChanged(nameof(reapply_patch));
                }
            }
        }
        #endregion

        #region failed_to_receive_patches
        private string _failed_to_receive_patches;
        public string failed_to_receive_patches
        {
            get => _failed_to_receive_patches;
            set
            {
                if (_failed_to_receive_patches != value)
                {
                    _failed_to_receive_patches = value;
                    RaisePropertyChanged(nameof(failed_to_receive_patches));
                }
            }
        }
        #endregion

        #region failed_core_patch
        private string _failed_core_patch;
        public string failed_core_patch
        {
            get => _failed_core_patch;
            set
            {
                if (_failed_core_patch != value)
                {
                    _failed_core_patch = value;
                    RaisePropertyChanged(nameof(failed_core_patch));
                }
            }
        }
        #endregion

        #region failed_mod_patch
        private string _failed_mod_patch;
        public string failed_mod_patch
        {
            get => _failed_mod_patch;
            set
            {
                if (_failed_mod_patch != value)
                {
                    _failed_mod_patch = value;
                    RaisePropertyChanged(nameof(failed_mod_patch));
                }
            }
        }
        #endregion

        #region OK
        private string _ok;
        public string ok
        {
            get => _ok;
            set
            {
                if (_ok != value)
                {
                    _ok = value;
                    RaisePropertyChanged(nameof(ok));
                }
            }
        }
        #endregion

        #region account_page_denied
        private string _account_page_denied;
        public string account_page_denied
        {
            get => _account_page_denied;
            set
            {
                if (_account_page_denied != value)
                {
                    _account_page_denied = value;
                    RaisePropertyChanged(nameof(account_page_denied));
                }
            }
        }
        #endregion

        #region account_updated
        private string _account_updated;
        public string account_updated
        {
            get => _account_updated;
            set
            {
                if (_account_updated != value)
                {
                    _account_updated = value;
                    RaisePropertyChanged(nameof(_account_updated));
                }
            }
        }
        #endregion

        #region nickname
        private string _nickname;
        public string nickname
        {
            get => _nickname;
            set
            {
                if (_nickname != value)
                {
                    _nickname = value;
                    RaisePropertyChanged(nameof(nickname));
                }
            }
        }
        #endregion

        #region side
        private string _side;
        public string side
        {
            get => _side;
            set
            {
                if (_side != value)
                {
                    _side = value;
                    RaisePropertyChanged(nameof(side));
                }
            }
        }
        #endregion

        #region level
        private string _level;
        public string level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    RaisePropertyChanged(nameof(level));
                }
            }
        }
        #endregion

        #region patching
        private string _patching;
        public string patching
        {
            get => _patching;
            set
            {
                if(_patching != value)
                {
                    _patching = value;
                    RaisePropertyChanged(nameof(patching));
                }
            }
        }
        #endregion

        #region file_mismatch_dialog_message
        private string _file_mismatch_dialog_message;
        public string file_mismatch_dialog_message
        {
            get => _file_mismatch_dialog_message;
            set
            {
                if(_file_mismatch_dialog_message != value)
                {
                    _file_mismatch_dialog_message = value;
                    RaisePropertyChanged(nameof(file_mismatch_dialog_message));
                }
            }
        }
        #endregion

        #region yes
        private string _yes;
        public string yes
        {
            get => _yes;
            set
            {
                if(_yes != value)
                {
                    _yes = value;
                    RaisePropertyChanged(nameof(yes));
                }
            }
        }
        #endregion

        #region no
        private string _no;
        public string no
        {
            get => _no;
            set
            {
                if(_no != value)
                {
                    _no = value;
                    RaisePropertyChanged(nameof(no));
                }
            }
        }
        #endregion

        #region profile_created
        private string _profile_created;
        public string profile_created
        {
            get => _profile_created;
            set
            {
                if(_profile_created != value)
                {
                    _profile_created = value;
                    RaisePropertyChanged(nameof(profile_created));
                }
            }
        }
        #endregion

        #region open_folder
        private string _open_folder;
        public string open_folder
        {
            get => _open_folder;
            set
            {
                if(_open_folder != value)
                {
                    _open_folder = value;
                    RaisePropertyChanged(nameof(open_folder));
                }
            }
        }
        #endregion

        #region select_edition
        private string _select_edition;
        public string select_edition
        {
            get => _select_edition;
            set
            {
                if(_select_edition != value)
                {
                    _select_edition = value;
                    RaisePropertyChanged(nameof(select_edition));
                }
            }
        }
        #endregion

        #region copied
        private string _copied;
        public string copied
        {
            get => _copied;
            set
            {
                if(_copied != value)
                {
                    _copied = value;
                    RaisePropertyChanged(nameof(copied));
                }
            }
        }
        #endregion

        #region next_level_in
        private string _next_level_in;
        public string next_level_in
        {
            get => _next_level_in;
            set
            {
                if(_next_level_in != value)
                {
                    _next_level_in = value;
                    RaisePropertyChanged(nameof(next_level_in));
                }
            }
        }
        #endregion

        #region no_profile_data
        private string _no_profile_data;
        public string no_profile_data
        {
            get => _no_profile_data;
            set
            {
                if (_no_profile_data != value)
                {
                    _no_profile_data = value;
                    RaisePropertyChanged(nameof(no_profile_data));
                }
            }
        }
        #endregion

        #region profile_version_mismatch
        private string _profile_version_mismath;
        public string profile_version_mismath
        {
            get => _profile_version_mismath;
            set
            {
                if(_profile_version_mismath != value)
                {
                    _profile_version_mismath = value;
                    RaisePropertyChanged(nameof(profile_version_mismath));
                }
            }
        }
        #endregion

        #region profile_removed
        private string _profile_removed;
        public string profile_removed
        {
            get => _profile_removed;
            set
            {
                if(_profile_removed != value)
                {
                    _profile_removed = value;
                    RaisePropertyChanged(nameof(profile_removed));
                }
            }
        }
        #endregion

        #region profile_removal_failed
        private string _profile_removal_failed;
        public string profile_removal_failed
        {
            get => _profile_removal_failed;
            set
            {
                if(_profile_removal_failed != value)
                {
                    _profile_removal_failed = value;
                    RaisePropertyChanged(nameof(profile_removal_failed));
                }
            }
        }
        #endregion

        #region profile_remove_question_format_1
        private string _profile_remove_question_format_1;
        public string profile_remove_question_format_1
        {
            get => _profile_remove_question_format_1;
            set
            {
                if(_profile_remove_question_format_1 != value)
                {
                    _profile_remove_question_format_1 = value;
                    RaisePropertyChanged(nameof(profile_remove_question_format_1));
                }
            }
        }
        #endregion

        #region i_understand
        private string _i_understand;
        public string i_understand
        {
            get => _i_understand;
            set
            {
                if(_i_understand != value)
                {
                    _i_understand = value;
                    RaisePropertyChanged(nameof(i_understand));
                }
            }
        }
        #endregion

        #region game_version_mismatch_format_2
        private string _game_version_mismatch_format_2;
        public string game_version_mismatch_format_2
        {
            get => _game_version_mismatch_format_2;
            set
            {
                if(_game_version_mismatch_format_2 != value)
                {
                    _game_version_mismatch_format_2 = value;
                    RaisePropertyChanged(nameof(game_version_mismatch_format_2));
                }
            }
        }
        #endregion

        #region description
        private string _description;
        public string description
        {
            get => _description;
            set
            {
                if(_description != value)
                {
                    _description = value;
                    RaisePropertyChanged(nameof(description));
                }
            }
        }
        #endregion

        #region author
        private string _author;
        public string author
        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    _author = value;
                    RaisePropertyChanged(nameof(author));
                }
            }
        }

        #endregion

        #region wipe_on_start
        private string _wipe_on_start;
        public string wipe_on_start
        {
            get => _wipe_on_start;
            set
            {
                if (_wipe_on_start != value)
                {
                    _wipe_on_start = value;
                    RaisePropertyChanged(nameof(wipe_on_start));
                }
            }
        }
        #endregion

        #region copy_live_settings_question
        private string _copy_live_settings_question;
        public string copy_live_settings_question
        {
            get => _copy_live_settings_question;
            set
            {
                if (_copy_live_settings_question != value)
                {
                    _copy_live_settings_question = value;
                    RaisePropertyChanged(nameof(copy_live_settings_question));
                }
            }
        }
        #endregion
        
        #region mod_not_in_server_warning
        private string _mod_not_in_server_warning;
        public string mod_not_in_server_warning
        {
            get => _mod_not_in_server_warning;
            set
            {
                if (_mod_not_in_server_warning != value)
                {
                    _mod_not_in_server_warning = value;
                    RaisePropertyChanged(nameof(mod_not_in_server_warning));
                }
            }
        }
        #endregion
        
        #region active_server_mods
        private string _active_server_mods;
        public string active_server_mods
        {
            get => _active_server_mods;
            set
            {
                if (_active_server_mods != value)
                {
                    _active_server_mods = value;
                    RaisePropertyChanged(nameof(active_server_mods));
                }
            }
        }
        #endregion
        
        #region active_server_mods_info_text

        private string _active_server_mods_info_text;

        public string active_server_mods_info_text
        {
            get => _active_server_mods_info_text;
            set
            {
                if (_active_server_mods_info_text != value)
                {
                    _active_server_mods_info_text = value;
                    RaisePropertyChanged(nameof(active_server_mods_info_text));
                }
            }
        }
        #endregion
        
        #region inactive_server_mods
        private string _inactive_server_mods;
        public string inactive_server_mods
        {
            get => _inactive_server_mods;
            set
            {
                if (_inactive_server_mods != value)
                {
                    _inactive_server_mods = value;
                    RaisePropertyChanged(nameof(inactive_server_mods));
                }
            }
        }
        #endregion
        
        #region inactive_server_mods_info_text

        private string _inactive_server_mods_info_text;

        public string inactive_server_mods_info_text
        {
            get => _inactive_server_mods_info_text;
            set
            {
                if (_inactive_server_mods_info_text != value)
                {
                    _inactive_server_mods_info_text = value;
                    RaisePropertyChanged(nameof(inactive_server_mods_info_text));
                }
            }
        }
        #endregion
        
        #region open_link_question_format_1

        private string _open_link_question_format_1;

        public string open_link_question_format_1
        {
            get => _open_link_question_format_1;
            set
            {
                if (_open_link_question_format_1 != value)
                {
                    _open_link_question_format_1 = value;
                    RaisePropertyChanged(nameof(open_link_question_format_1));
                }
            }
        }
        #endregion
        
        #region open_link

        private string _open_link;

        public string open_link
        {
            get => _open_link;
            set
            {
                if (_open_link != value)
                {
                    _open_link = value;
                    RaisePropertyChanged(nameof(open_link));
                }
            }
        }
        #endregion

        #region core_dll_file_version_mismatch
        private string _core_dll_file_version_mismatch;

        public string core_dll_file_version_mismatch
        {
            get => _core_dll_file_version_mismatch;
            set
            {
                if (_core_dll_file_version_mismatch != value)
                {
                    _core_dll_file_version_mismatch = value;
                    RaisePropertyChanged(nameof(core_dll_file_version_mismatch));
                }
            }
        }

        #endregion

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
