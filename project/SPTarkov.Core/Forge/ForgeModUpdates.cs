using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public class ForgeModUpdates
{
    [JsonPropertyName("spt_version")]
    public string SptVersion { get; set; } = "0.0.0";

    [JsonPropertyName("updates")]
    public required List<ForgeModUpdate> Updates { get; set; }

    [JsonPropertyName("blocked_updates")]
    public required List<ForgeModUpdate> BlockedUpdates { get; set; }

    [JsonPropertyName("up_to_date")]
    public required List<ForgeModUpdate> UpToDate { get; set; }

    [JsonPropertyName("incompatible_with_spt")]
    public required List<ForgeModUpdate> IncompatibleWithSpt { get; set; }
}
