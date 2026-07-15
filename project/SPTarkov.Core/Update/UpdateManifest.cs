using System.Text.Json.Serialization;

namespace SPTarkov.Core.Update;

public record UpdateManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    [JsonPropertyName("generatedUtc")]
    public DateTimeOffset GeneratedUtc { get; init; }

    [JsonPropertyName("releases")]
    public List<ReleaseEntry> Releases { get; init; } = new();
}
