/* AccountManager.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Controllers;
using SPT.Launcher.Helpers;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.Launcher;
using SPT.Launcher.Models.SPT;
using System.Threading.Tasks;

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

        public static void Logout()
        {
            // Set currently selected account to null, as well as removing the old session token
            SelectedAccount = null;
            RequestHandler.ChangeSession(null);
        }

        public static async Task<AccountStatus> LoginAsync(LoginModel Creds)
        {
            return await LoginAsync(Creds.Username);
        }

        public static async Task<AccountStatus> LoginAsync(string username)
        {
            LoginRequestData data = new LoginRequestData(username);
            string id = STATUS_FAILED;
            string json = "";

            try
            {
                id = await RequestHandler.RequestLogin(data);

                if (id == STATUS_FAILED)
                {
                    return AccountStatus.LoginFailed;
                }

                json = await RequestHandler.RequestAccount(data);
            }
            catch
            {
                return AccountStatus.NoConnection;
            }

            SelectedAccount = Json.Deserialize<AccountInfo>(json);
            RequestHandler.ChangeSession(SelectedAccount.id);

            await UpdateProfileInfoAsync();

            return AccountStatus.OK;
        }

        public static async Task UpdateProfileInfoAsync()
        {
            LoginRequestData data = new LoginRequestData(SelectedAccount.username);
            string profileInfoJson = await RequestHandler.RequestProfileInfo(data);

            if (profileInfoJson != null)
            {
                ServerProfileInfo serverProfileInfo = Json.Deserialize<ServerProfileInfo>(profileInfoJson);
                SelectedProfileInfo = new ProfileInfo(serverProfileInfo);
            }
        }

        public static async Task<ServerProfileInfo[]> GetExistingProfilesAsync()
        {
            string profileJsonArray = await RequestHandler.RequestExistingProfiles();

            if(profileJsonArray != null)
            {
                ServerProfileInfo[] miniProfiles = Json.Deserialize<ServerProfileInfo[]>(profileJsonArray);

                if (miniProfiles != null && miniProfiles.Length > 0)
                {
                    return miniProfiles;
                }
            }

            return [];
        }

        public static async Task<AccountStatus> RegisterAsync(string username, string edition)
        {
            string registerResult;
            try
            {
                registerResult = await RequestHandler.RequestRegister(new RegisterRequestData(username, edition));
            }
            catch
            {
                return AccountStatus.NoConnection;
            }

            if (registerResult == string.Empty)
            {
                return AccountStatus.RegisterFailed;
            }

            LogManager.Instance.Info($"Account Registered: {username} {registerResult}");

            return await LoginAsync(username);
        }

        public static async Task<AccountStatus> RemoveAsync()
        {
            LoginRequestData data = new LoginRequestData(SelectedAccount.username);

            try
            {
                string json = await RequestHandler.RequestRemove(data);

                if(Json.Deserialize<bool>(json))
                {
                    // Set currently selected account to null, as well as removing the old session token
                    SelectedAccount = null;
                    RequestHandler.ChangeSession(null);

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
            ChangeRequestData data = new ChangeRequestData(SelectedAccount.username, username);
            string json = STATUS_FAILED;

            try
            {
                json = await RequestHandler.RequestChangeUsername(data);

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

        public static async Task<AccountStatus> WipeAsync(string edition)
        {
            RegisterRequestData data = new RegisterRequestData(SelectedAccount.username, edition);
            string json = STATUS_FAILED;

            try
            {
                json = await RequestHandler.RequestWipe(data);

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
