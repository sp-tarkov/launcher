using System.Collections.Generic;

namespace SPT.Launcher.Models.SPT
{
    public class SPTServerModInfo : SPTMod
    {
        public string ModGuid { get; set; }
        public string License { get; set; }
        public string SPTVersion { get; set; }
    }
}

/*
{
    "Live Flea Prices": {
        "ModGuid": "xyz.drakia.livefleaprices",
        "Name": "Live Flea Prices",
        "Author": "DrakiaXYZ",
        "Version": "2.0.0",
        "SptVersion": "~4.0.0",
        "License": "MIT"
    }
}
*/