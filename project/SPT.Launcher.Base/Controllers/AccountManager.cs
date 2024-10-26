/* AccountManager.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using System;
using SPT.Launcher.Helpers;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.SPT;
using SPT.Launcher.Models.Launcher;
using System.Threading.Tasks;
using SPT.Launcher.Controllers;

namespace SPT.Launcher
{
    public enum AccountStatus
    {
        OK = 0,
        NoConnection = 1,
        LoginFailed = 2,
        RegisterFailed = 3,
        UpdateFailed = 4
    }

    public static class AccountManager
    {
        private const string STATUS_FAILED = "FAILED";
        private const string STATUS_OK = "OK";
        public static AccountInfo SelectedAccount { get; private set; } = null;
        public static ProfileInfo SelectedProfileInfo { get; private set; } = null;

        public static void Logout() => SelectedAccount = null;

        public static async Task<AccountStatus> LoginAsync(LoginModel Creds)
        {
            return await Task.Run(() =>
            {
                return Login(Creds.Username, Creds.Password);
            });
        }

        public static async Task<AccountStatus> LoginAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                return Login(username, password);
            });
        }

        public static AccountStatus Login(string username, string password)
        {
            LoginRequestData data = new LoginRequestData(username, password);
            string id = STATUS_FAILED;
            string json = "";

            try
            {
                id = RequestHandler.RequestLogin(data);

                if (id == STATUS_FAILED)
                {
                    return AccountStatus.LoginFailed;
                }

                json = RequestHandler.RequestAccount(data);
            }
            catch
            {
                return AccountStatus.NoConnection;
            }

            SelectedAccount = Json.Deserialize<AccountInfo>(json);
            RequestHandler.ChangeSession(SelectedAccount.id);

            UpdateProfileInfo();

            return AccountStatus.OK;
        }

        public static void UpdateProfileInfo()
        {
            LoginRequestData data = new LoginRequestData(SelectedAccount.username, SelectedAccount.password);
            string profileInfoJson = RequestHandler.RequestProfileInfo(data);

            if (profileInfoJson != null)
            {
                ServerProfileInfo serverProfileInfo = Json.Deserialize<ServerProfileInfo>(profileInfoJson);
                SelectedProfileInfo = new ProfileInfo(serverProfileInfo);
            }
        }

        public static ServerProfileInfo[] GetExistingProfiles()
        {
            string profileJsonArray = RequestHandler.RequestExistingProfiles();

            if(profileJsonArray != null)
            {
                ServerProfileInfo[] miniProfiles = Json.Deserialize<ServerProfileInfo[]>(profileJsonArray);

                if (miniProfiles != null && miniProfiles.Length > 0)
                {
                    return miniProfiles;
                }
            }

            return new ServerProfileInfo[0];
        }

        public static async Task<AccountStatus> RegisterAsync(string username, string password, string edition)
        {
            return await Task.Run(() =>
            {
                return Register(username, password, edition);
            });
        }

        public static AccountStatus Register(string username, string password, string edition)
        {
            RegisterRequestData data = new RegisterRequestData(username, password, edition);
            string registerStatus = STATUS_FAILED;

            try
            {
                registerStatus = RequestHandler.RequestRegister(data);

                if (registerStatus != STATUS_OK)
                {
                    return AccountStatus.RegisterFailed;
                }
            }
            catch
            {
                return AccountStatus.NoConnection;
            }
            
            LogManager.Instance.Info($"Account Registered: {username}");

            return Login(username, password);
        }

        //only added incase wanted for future use.
        public static async Task<AccountStatus> RemoveAsync()
        {
            return await Task.Run(() =>
            {
                return Remove();
            });
        }

        public static AccountStatus Remove()
        {
            LoginRequestData data = new LoginRequestData(SelectedAccount.username, SelectedAccount.password);

            try
            {
                string json = RequestHandler.RequestRemove(data);

                if(Json.Deserialize<bool>(json))
                {
                    SelectedAccount = null;
                    
                    LogManager.Instance.Info($"Account Removed: {data.username}");

                    return AccountStatus.OK;
                }
                else
                {
                    LogManager.Instance.Error($"Failed to remove account: {data.username}");
                    return AccountStatus.UpdateFailed;
                }
            }
            catch
            {
                LogManager.Instance.Error($"Failed to remove account: {data.username} - NO CONNECTION");
                return AccountStatus.NoConnection;
            }
        }

        public static async Task<AccountStatus> ChangeUsernameAsync(string username)
        {
            return await Task.Run(() =>
            {
                return ChangeUsername(username);
            });
        }

        public static AccountStatus ChangeUsername(string username)
        {
            ChangeRequestData data = new ChangeRequestData(SelectedAccount.username, SelectedAccount.password, username);
            string json = STATUS_FAILED;

            try
            {
                json = RequestHandler.RequestChangeUsername(data);

                if (json != STATUS_OK)
                {
                    return AccountStatus.UpdateFailed;
                }
            }
            catch
            {
                return AccountStatus.NoConnection;
            }

            ServerSetting DefaultServer = LauncherSettingsProvider.Instance.Server;

            if (DefaultServer.AutoLoginCreds != null)
            {
                DefaultServer.AutoLoginCreds.Username = username;
            }

            SelectedAccount.username = username;
            LauncherSettingsProvider.Instance.SaveSettings();

            return AccountStatus.OK;
        }

        public static async Task<AccountStatus> ChangePasswordAsync(string password)
        {
            return await Task.Run(() =>
            {
                return ChangePassword(password);
            });
        }
        public static AccountStatus ChangePassword(string password)
        {
            ChangeRequestData data = new ChangeRequestData(SelectedAccount.username, SelectedAccount.password, password);
            string json = STATUS_FAILED;

            try
            {
                json = RequestHandler.RequestChangePassword(data);

                if (json != STATUS_OK)
                {
                    return AccountStatus.UpdateFailed;
                }
            }
            catch
            {
                return AccountStatus.NoConnection;
            }

            ServerSetting DefaultServer = LauncherSettingsProvider.Instance.Server;

            if (DefaultServer.AutoLoginCreds != null)
            {
                DefaultServer.AutoLoginCreds.Password = password;
            }

            SelectedAccount.password = password;
            LauncherSettingsProvider.Instance.SaveSettings();

            return AccountStatus.OK;
        }

        public static async Task<AccountStatus> WipeAsync(string edition)
        {
            return await Task.Run(() =>
            {
                return Wipe(edition);
            });
        }

        public static AccountStatus Wipe(string edition)
        {
            RegisterRequestData data = new RegisterRequestData(SelectedAccount.username, SelectedAccount.password, edition);
            string json = STATUS_FAILED;

            try
            {
                json = RequestHandler.RequestWipe(data);

                if (json != STATUS_OK)
                {
                    LogManager.Instance.Error($"Failed to wipe account: {data.username}");
                    return AccountStatus.UpdateFailed;
                }
            }
            catch
            {
                LogManager.Instance.Error($"Failed to wipe account: {data.username} - NO CONNECTION");
                return AccountStatus.NoConnection;
            }

            SelectedAccount.edition = edition;
            SelectedAccount.wipe = true;
            LogManager.Instance.Info($"Account Wiped: {data.username} -> {edition}");
            return AccountStatus.OK;
        }
    }
}
