using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Forge;

public record ForgeDependencyNode
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("guid")]
    public string? GUID { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("latest_compatible_version")]
    public ForgeDependencyVersion? LatestCompatibleVersion { get; set; }

    [JsonPropertyName("conflict")]
    public bool Conflict { get; set; }

    [JsonPropertyName("dependencies")]
    public List<ForgeDependencyNode>? Dependencies { get; set; }
}

public record ForgeDependencyVersion
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("version")]
    [JsonConverter(typeof(SemVerVersionConverter))]
    public Version? Version { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("content_length")]
    public long? ContentLength { get; set; }

    [JsonPropertyName("fika_compatibility")]
    public string? FikaCompatibility { get; set; }
}
