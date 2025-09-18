using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class SPTVersion
{
    public SPTVersion(
        string spt = "",
        string eft = ""
    )
    {
        SptVersion = spt;
        EftVersion = eft;
    }

    [JsonPropertyName("sptVersion")] public string SptVersion { get; set; }

    [JsonPropertyName("eftVersion")] public string EftVersion { get; set; }
}
