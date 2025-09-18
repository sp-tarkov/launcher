using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class SPTData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
