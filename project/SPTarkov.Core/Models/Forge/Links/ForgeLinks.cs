using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeLinks
{
    [JsonPropertyName("first")]
    public string? First { get; set; }

    [JsonPropertyName("last")]
    public string? Last { get; set; }

    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}
