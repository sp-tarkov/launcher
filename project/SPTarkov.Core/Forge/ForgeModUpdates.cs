using System.ComponentModel;
using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Forge;

public class ForgeModUpdates
{
    [JsonPropertyName("spt_version")]
    [JsonConverter(typeof(SemVerVersionConverter))]
    public Version SptVersion { get; set; } = new Version(0, 0, 0);

    [JsonPropertyName("updates")]
    public required List<ForgeModUpdate> Updates { get; set; }

    [JsonPropertyName("blocked_updates")]
    public required List<ForgeModUpdate> BlockedUpdates { get; set; }

    [JsonPropertyName("up_to_date")]
    public required List<UpdateMod> UpToDate { get; set; }

    [JsonPropertyName("incompatible_with_spt")]
    public required List<ForgeModUpdate> IncompatibleWithSpt { get; set; }
}
