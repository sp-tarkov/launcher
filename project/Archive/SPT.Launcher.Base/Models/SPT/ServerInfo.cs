/* ServerInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using System.Collections.Generic;

namespace SPT.Launcher
{
    public class ServerInfo
    {
        public string backendUrl;
        public string name;
        public string[] editions;
        public Dictionary<string, string> profileDescriptions;

        public ServerInfo()
        {
            backendUrl = "https://127.0.0.1:6969";
            name = "Local SPT Server";
            editions = new string[0];
            profileDescriptions = new Dictionary<string, string>();
        }
    }
}
