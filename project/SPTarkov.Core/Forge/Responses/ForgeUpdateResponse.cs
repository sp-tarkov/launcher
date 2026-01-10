using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public class ForgeUpdateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public ForgeModUpdates? Data { get; init; }
}
