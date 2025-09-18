using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeModIncludes
{
    [JsonPropertyName("users")] public List<ForgeBase> Users { get; set; }

    [JsonPropertyName("license")] public ForgeBase License { get; set; }

    [JsonPropertyName("versions")] public List<ForgeBase> Versions { get; set; }
}
