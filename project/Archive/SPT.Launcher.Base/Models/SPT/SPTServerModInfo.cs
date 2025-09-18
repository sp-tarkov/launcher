using System.Collections.Generic;

namespace SPT.Launcher.Models.SPT
{
    public class SPTServerModInfo : SPTMod
    {
        public string Main { get; set; }
        public string License { get; set; }
        public string SPTVersion { get; set; }
        public Dictionary<string, string> Scripts { get; set; }
        public DevDependencies DevDependencies { get; set; }
    }
}

/*
{
    "wafflelord-ZeroToHeroPlus-1.0.0": {
        "name": "ZeroToHeroPlus",
        "version": "1.0.0",
        "main": "src/mod.ts",
        "license": "MIT",
        "author": "waffle.lord",
        "SPTVersion": "~3.6",
        "scripts": {
            "setup": "npm i",
            "build": "node ./packageBuild.ts"
        },
        "devDependencies": {
            "@types/node": "16.18.10",
            "@typescript-eslint/eslint-plugin": "5.46.1",
            "@typescript-eslint/parser": "5.46.1",
            "bestzip": "2.2.1",
            "eslint": "8.30.0",
            "fs-extra": "11.1.0",
            "glob": "8.0.3",
            "tsyringe": "4.7.0",
            "typescript": "4.9.4"
        }
    }
}
*/