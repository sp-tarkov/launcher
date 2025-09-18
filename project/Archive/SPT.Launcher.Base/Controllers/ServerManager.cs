/* ServerManager.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.SPT;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPT.Launcher
{
    public static class ServerManager
    {
        public static ServerInfo SelectedServer { get; private set; } = null;

        public static async Task<bool> PingServerAsync()
        {
            string json = "";

            try
            {
                json = await RequestHandler.SendPing();

                if (json != null) return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static async Task<string> GetVersionAsync()
        {
            try
            {
                string json = await RequestHandler.RequestServerVersion();

                return Json.Deserialize<string>(json);
            }
            catch
            {
                return "";
            }
        }

        public static async Task<string> GetCompatibleGameVersionAsync()
        {
            try
            {
                string json = await RequestHandler.RequestCompatibleGameVersion();

                return Json.Deserialize<string>(json);
            }
            catch
            {
                return "";
            }
        }

        public static async Task<Dictionary<string, SPTServerModInfo>> GetLoadedServerModsAsync()
        {
            try
            {
                string json = await RequestHandler.RequestLoadedServerMods();

                return Json.Deserialize<Dictionary<string, SPTServerModInfo>>(json);
            }
            catch
            {
                return new Dictionary<string, SPTServerModInfo>();
            }
        }

        public static async Task<SPTProfileModInfo[]> GetProfileModsAsync()
        {
            try
            {
                string json = await RequestHandler.RequestProfileMods();

                return Json.Deserialize<SPTProfileModInfo[]>(json);
            }
            catch
            {
                return [];
            }
        }

        public static async Task<bool> LoadServerAsync(string backendUrl)
        {
            string json = "";

            try
            {
                RequestHandler.ChangeBackendUrl(backendUrl);
                json = await RequestHandler.RequestConnect();
                SelectedServer = Json.Deserialize<ServerInfo>(json);
            }
            catch
            {
                SelectedServer = null;
                return false;
            }

            return true;
        }
    }
}
