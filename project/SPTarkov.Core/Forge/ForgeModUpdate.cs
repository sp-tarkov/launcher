using System.ComponentModel;
using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Forge;

public class ForgeModUpdate
{
    [JsonPropertyName("current_version")]
    public UpdateMod CurrentVersion { get; set; }

    [JsonPropertyName("recommended_version")]
    public UpdateMod RecommendedVersion { get; set; }

    [JsonPropertyName("update_reason")]
    public string UpdateReason { get; set; }
}

public class UpdateMod
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("mod_id")]
    public int? ModId { get; set; }

    [JsonPropertyName("guid")]
    public string? GUID { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; } = "";

    [JsonPropertyName("slug")]
    public string? Slug { get; set; } = "";

    [JsonPropertyName("version")]
    [JsonConverter(typeof(SemVerConverter))]
    public Version? Version { get; set; } = new Version(0, 0, 0);

    [JsonPropertyName("link")]
    public string? Link { get; set; } = "";

    [JsonPropertyName("content_length")]
    public long? ContentLength { get; set; }

    [JsonPropertyName("fika_compatibility")]
    public string? FikaCompatibility { get; set; }

    [JsonPropertyName("spt_versions")]
    public List<Version>? SptVersions { get; set; }
}
