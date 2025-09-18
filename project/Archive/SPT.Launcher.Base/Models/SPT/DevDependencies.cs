using Newtonsoft.Json;

namespace SPT.Launcher.Models.SPT
{
    public class DevDependencies
    {
        [JsonProperty("@types/node")]
        public string TypesNode { get; set; }

        [JsonProperty("@typescript-eslint/eslint-plugin")]
        public string EslintPlugin { get; set; }

        [JsonProperty("@typescript-eslint/parser")]
        public string EslintParser { get; set; }
        public string BestZip { get; set; }
        public string Eslint { get; set; }

        [JsonProperty("fs-extra")]
        public string FsExtra { get; set; }
        public string Glob { get; set; }
        public string Tsyringe { get; set; }
        public string Typescript { get; set; }

    }
}

/*
            "@types/node": "16.18.10",
            "@typescript-eslint/eslint-plugin": "5.46.1",
            "@typescript-eslint/parser": "5.46.1",
            "bestzip": "2.2.1",
            "eslint": "8.30.0",
            "fs-extra": "11.1.0",
            "glob": "8.0.3",
            "tsyringe": "4.7.0",
            "typescript": "4.9.4"
*/