using System.Text.Json.Serialization;

namespace SPTarkov.Core.Update;

public record ReleaseAsset
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}
