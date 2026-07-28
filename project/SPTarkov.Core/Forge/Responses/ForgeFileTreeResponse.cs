using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public record ForgeFileTreeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public ForgeFileTree? Data { get; init; }
}
