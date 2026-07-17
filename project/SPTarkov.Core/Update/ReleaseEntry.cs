using System.Text.Json.Serialization;

namespace SPTarkov.Core.Update;

public record ReleaseEntry
{
    [JsonPropertyName("launcherVersion")]
    public required string LauncherVersion { get; init; }

    [JsonPropertyName("sptVersion")]
    public required string SptVersion { get; init; }

    [JsonPropertyName("channel")]
    public required string Channel { get; init; }

    [JsonPropertyName("tag")]
    public required string Tag { get; init; }

    [JsonPropertyName("commit")]
    public string? Commit { get; init; }

    [JsonPropertyName("publishedUtc")]
    public DateTimeOffset PublishedUtc { get; init; }

    [JsonPropertyName("yanked")]
    public bool Yanked { get; init; }

    [JsonPropertyName("deltaSha256")]
    public string? DeltaSha256 { get; init; }

    [JsonPropertyName("notesUrl")]
    public string? NotesUrl { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>The payload archive holding both platform binaries.</summary>
    [JsonPropertyName("asset")]
    public required ReleaseAsset Asset { get; init; }
}
