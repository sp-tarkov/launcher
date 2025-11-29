using System.Text.Json.Serialization;

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
    public string? Version { get; set; } = "";

    [JsonPropertyName("link")]
    public string? Link { get; set; } = "";

    [JsonPropertyName("content_length")]
    public long? ContentLength { get; set; }

    [JsonPropertyName("fika_compatibility")]
    public string? FikaCompatibility { get; set; }

    [JsonPropertyName("spt_versions")]
    public List<string>? SptVersions { get; set; }
}
