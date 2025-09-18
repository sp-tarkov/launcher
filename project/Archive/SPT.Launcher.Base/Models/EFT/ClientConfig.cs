/* ClientConfig.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


namespace SPT.Launcher
{
    public class ClientConfig
    {
        public string BackendUrl;
        public string MatchingVersion;
        public string Version;

        public ClientConfig()
        {
            BackendUrl = "https://127.0.0.1:6969";
            Version = "live";
            MatchingVersion = "live";
        }

        public ClientConfig(string backendUrl)
        {
            BackendUrl = backendUrl;
            Version = "live";
            MatchingVersion = "live";
        }
    }
}
