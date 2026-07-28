using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public record ForgeFileTree
{
    [JsonPropertyName("verified_at")]
    public string? VerifiedAt { get; init; }

    [JsonPropertyName("file_count")]
    public int? FileCount { get; init; }

    [JsonPropertyName("truncated")]
    public bool Truncated { get; init; }

    [JsonPropertyName("files")]
    public List<string>? Files { get; init; }
}
